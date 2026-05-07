using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(RecordAttemptsCollection))]
public sealed class RecordAttemptTests(CollectionFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private int _meetId;
    private string _meetSlug = string.Empty;
    private int _participationId;

    public async ValueTask InitializeAsync()
    {
        CreateMeetCommand meetCommand = new CreateMeetCommandBuilder().Build();

        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync("/meets", meetCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        _meetSlug = createResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meetSlug}", CancellationToken.None);
        _meetId = details!.MeetId;

        _participationId = await AddParticipantAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_meetId != 0)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{_meetSlug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
        _nonAdminHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenCreatingNewAttempt()
    {
        // Arrange
        int participationId = await AddParticipantAsync();
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, participationId, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNoContent_WhenUpdatingExistingAttempt()
    {
        // Arrange
        int participationId = await AddParticipantAsync();
        await RecordAttempt(participationId, Discipline.Squat, 1, 100.0m, true);

        RecordAttemptCommand updateCommand = new RecordAttemptCommandBuilder()
            .WithWeight(110.0m)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, participationId, Discipline.Squat, 1),
            updateCommand,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TotalsRecalculated_AfterAttemptRecorded()
    {
        // Arrange
        int participationId = await AddParticipantAsync();

        await RecordAttempt(participationId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttempt(participationId, Discipline.Squat, 2, 110.0m, true);
        await RecordAttempt(participationId, Discipline.Bench, 1, 60.0m, true);
        await RecordAttempt(participationId, Discipline.Bench, 2, 70.0m, true);
        await RecordAttempt(participationId, Discipline.Deadlift, 1, 150.0m, true);

        // Act
        await RecordAttempt(participationId, Discipline.Deadlift, 2, 160.0m, true);

        // Assert - best good lifts: Squat=110, Bench=70, Deadlift=160, Total=340
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{_meetSlug}/participations",
                CancellationToken.None);

        participations.ShouldNotBeNull();
        participations.ShouldContain(p => p.Total == 340.0m);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationDoesNotExist()
    {
        // Arrange
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, 99999, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Arrange
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(99999, _participationId, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, _participationId, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenNotAdmin()
    {
        // Arrange
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PutAsJsonAsync(
            Path(_meetId, _participationId, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenInvalidDiscipline()
    {
        // Arrange
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, _participationId, (Discipline)99, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenInvalidRound()
    {
        // Arrange
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, _participationId, Discipline.Squat, 4),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenWeightIsZeroOrNegative()
    {
        // Arrange
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(0m)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, _participationId, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BombedOut_WhenNoBenchGoodLifts()
    {
        // Arrange - all bench attempts are no-good
        int participationId = await AddParticipantAsync();

        await RecordAttempt(participationId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttempt(participationId, Discipline.Bench, 1, 60.0m, false);
        await RecordAttempt(participationId, Discipline.Bench, 2, 60.0m, false);
        await RecordAttempt(participationId, Discipline.Bench, 3, 60.0m, false);
        await RecordAttempt(participationId, Discipline.Deadlift, 1, 150.0m, true);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{_meetSlug}/participations",
                CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation? bombedOut = participations
            .FirstOrDefault(p => p.ParticipationId == participationId);

        bombedOut.ShouldNotBeNull();
        bombedOut.Total.ShouldBe(0m);
    }

    [Fact]
    public async Task AttemptPersistedThroughAggregate_WhenRecordedViaParticipation()
    {
        // Arrange
        int participationId = await AddParticipantAsync();

        // Act
        await RecordAttempt(participationId, Discipline.Squat, 1, 120.0m, true);

        // Assert — retrieve participation and verify attempt is persisted and retrievable
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{_meetSlug}/participations",
                CancellationToken.None);

        participations.ShouldNotBeNull();
        MeetParticipation? participation = participations
            .FirstOrDefault(p => p.ParticipationId == participationId);

        participation.ShouldNotBeNull();
        MeetAttempt? attempt = participation.Attempts
            .FirstOrDefault(a => a.Discipline == Discipline.Squat && a.Round == 1);

        attempt.ShouldNotBeNull();
        attempt.Weight.ShouldBe(120.0m);
        attempt.IsGood.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenLegacyRoundFourAttemptExists()
    {
        // Arrange — create participant, record rounds 1 and 2, then inject a legacy round 4 attempt
        // (weight 0, good = true) as the old system stored as a placeholder
        int participationId = await AddParticipantAsync();

        await RecordAttempt(participationId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttempt(participationId, Discipline.Squat, 2, 110.0m, true);

        await fixture.ExecuteSqlAsync(
            $"INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy) VALUES ({participationId}, 1, 4, 0, 1, 'seed', 'seed')");

        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(115.0m)
            .Build();

        // Act — editing round 3 must not be blocked by the legacy round 4 entry
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, participationId, Discipline.Squat, 3),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ComputesPlace_AfterAttemptRecorded()
    {
        // Arrange
        int participation1Id = await AddParticipantAsync();
        int participation2Id = await AddParticipantAsync();

        // Participant 1 — higher total (300kg): should be rank 1
        await RecordAttempt(participation1Id, Discipline.Squat, 1, 100.0m, true);
        await RecordAttempt(participation1Id, Discipline.Bench, 1, 50.0m, true);
        await RecordAttempt(participation1Id, Discipline.Deadlift, 1, 150.0m, true);

        // Participant 2 — lower total (240kg): should be rank 2
        await RecordAttempt(participation2Id, Discipline.Squat, 1, 80.0m, true);
        await RecordAttempt(participation2Id, Discipline.Bench, 1, 40.0m, true);
        await RecordAttempt(participation2Id, Discipline.Deadlift, 1, 120.0m, true);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{_meetSlug}/participations",
                CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation? p1 = participations.FirstOrDefault(p => p.ParticipationId == participation1Id);
        MeetParticipation? p2 = participations.FirstOrDefault(p => p.ParticipationId == participation2Id);

        p1.ShouldNotBeNull();
        p2.ShouldNotBeNull();
        p1.Rank.ShouldBe(1);
        p2.Rank.ShouldBe(2);
    }

    [Fact]
    public async Task PlaceNotComputed_WhenCalcPlacesFalse()
    {
        // Arrange
        (int meetId, string meetSlug) = await CreateMeetAsync(
            new CreateMeetCommandBuilder().WithCalcPlaces(false));

        int participation1Id = await AddParticipantToMeetAsync(meetId);
        int participation2Id = await AddParticipantToMeetAsync(meetId);

        await RecordAttemptForMeet(meetId, participation1Id, Discipline.Squat, 1, 100.0m, true);
        await RecordAttemptForMeet(meetId, participation1Id, Discipline.Bench, 1, 50.0m, true);
        await RecordAttemptForMeet(meetId, participation1Id, Discipline.Deadlift, 1, 150.0m, true);

        await RecordAttemptForMeet(meetId, participation2Id, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, participation2Id, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, participation2Id, Discipline.Deadlift, 1, 120.0m, true);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{meetSlug}/participations",
                CancellationToken.None);

        // Assert — Place defaults to -1; with CalcPlaces=false it must not be modified
        await _authorizedHttpClient.DeleteAsync($"/meets/{meetSlug}", CancellationToken.None);

        participations.ShouldNotBeNull();
        MeetParticipation? p1 = participations.FirstOrDefault(p => p.ParticipationId == participation1Id);
        MeetParticipation? p2 = participations.FirstOrDefault(p => p.ParticipationId == participation2Id);

        p1.ShouldNotBeNull();
        p2.ShouldNotBeNull();
        p1.Rank.ShouldBe(-1);
        p2.Rank.ShouldBe(-1);
    }

    [Fact]
    public async Task DisqualifiedParticipantReceivesPlaceZero_AfterBombOut()
    {
        // Arrange
        (int meetId, string meetSlug) = await CreateMeetAsync(new CreateMeetCommandBuilder());

        int dqParticipationId = await AddParticipantToMeetAsync(meetId);
        int rankedParticipationId = await AddParticipantToMeetAsync(meetId);

        // DQ participant — all bench attempts fail
        await RecordAttemptForMeet(meetId, dqParticipationId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttemptForMeet(meetId, dqParticipationId, Discipline.Bench, 1, 60.0m, false);
        await RecordAttemptForMeet(meetId, dqParticipationId, Discipline.Bench, 2, 60.0m, false);
        await RecordAttemptForMeet(meetId, dqParticipationId, Discipline.Bench, 3, 60.0m, false);
        await RecordAttemptForMeet(meetId, dqParticipationId, Discipline.Deadlift, 1, 150.0m, true);

        // Ranked participant — valid total
        await RecordAttemptForMeet(meetId, rankedParticipationId, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, rankedParticipationId, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, rankedParticipationId, Discipline.Deadlift, 1, 120.0m, true);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{meetSlug}/participations",
                CancellationToken.None);

        // Assert
        await _authorizedHttpClient.DeleteAsync($"/meets/{meetSlug}", CancellationToken.None);

        participations.ShouldNotBeNull();
        MeetParticipation? dq = participations.FirstOrDefault(p => p.ParticipationId == dqParticipationId);
        MeetParticipation? ranked = participations.FirstOrDefault(p => p.ParticipationId == rankedParticipationId);

        dq.ShouldNotBeNull();
        ranked.ShouldNotBeNull();
        dq.Rank.ShouldBe(0);
        ranked.Rank.ShouldBe(1);
    }

    [Fact]
    public async Task LighterParticipantRanksHigher_WhenTotalsAreEqual()
    {
        // Arrange
        (int meetId, string meetSlug) = await CreateMeetAsync(new CreateMeetCommandBuilder());

        // Lighter participant (75.0 kg) — same total as heavier, should win tiebreaker
        // Both use body weights above 74.01 to land in the same 83 kg weight category
        int lighterParticipationId = await AddParticipantToMeetAsync(meetId, bodyWeight: 75.0m);

        // Heavier participant (80.0 kg)
        int heavierParticipationId = await AddParticipantToMeetAsync(meetId, bodyWeight: 80.0m);

        // Both record the same total (240 kg)
        await RecordAttemptForMeet(meetId, lighterParticipationId, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, lighterParticipationId, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, lighterParticipationId, Discipline.Deadlift, 1, 120.0m, true);

        await RecordAttemptForMeet(meetId, heavierParticipationId, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, heavierParticipationId, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, heavierParticipationId, Discipline.Deadlift, 1, 120.0m, true);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{meetSlug}/participations",
                CancellationToken.None);

        // Assert
        await _authorizedHttpClient.DeleteAsync($"/meets/{meetSlug}", CancellationToken.None);

        participations.ShouldNotBeNull();
        MeetParticipation? lighter = participations.FirstOrDefault(p => p.ParticipationId == lighterParticipationId);
        MeetParticipation? heavier = participations.FirstOrDefault(p => p.ParticipationId == heavierParticipationId);

        lighter.ShouldNotBeNull();
        heavier.ShouldNotBeNull();
        lighter.Rank.ShouldBe(1);
        heavier.Rank.ShouldBe(2);
    }

    [Fact]
    public async Task TeamPoints_MapToTiebreakerPointValues_ByPlace()
    {
        // Arrange
        (int meetId, string meetSlug) = await CreateMeetAsync(new CreateMeetCommandBuilder());

        int higherTotalParticipationId = await AddParticipantToMeetAsync(meetId);
        int lowerTotalParticipationId = await AddParticipantToMeetAsync(meetId);

        // Higher total → place 1 → 12 TeamPoints
        await RecordAttemptForMeet(meetId, higherTotalParticipationId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttemptForMeet(meetId, higherTotalParticipationId, Discipline.Bench, 1, 50.0m, true);
        await RecordAttemptForMeet(meetId, higherTotalParticipationId, Discipline.Deadlift, 1, 150.0m, true);

        // Lower total → place 2 → 9 TeamPoints
        await RecordAttemptForMeet(meetId, lowerTotalParticipationId, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, lowerTotalParticipationId, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, lowerTotalParticipationId, Discipline.Deadlift, 1, 120.0m, true);

        // Act — read TeamPoints directly from the database (no read endpoint exposes per-participation TeamPoints)
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<Participation> participationEntities = await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == higherTotalParticipationId || p.ParticipationId == lowerTotalParticipationId)
            .ToListAsync(CancellationToken.None);

        // Assert
        await _authorizedHttpClient.DeleteAsync($"/meets/{meetSlug}", CancellationToken.None);

        Participation? higher = participationEntities.FirstOrDefault(p => p.ParticipationId == higherTotalParticipationId);
        Participation? lower = participationEntities.FirstOrDefault(p => p.ParticipationId == lowerTotalParticipationId);

        higher.ShouldNotBeNull();
        lower.ShouldNotBeNull();
        higher.TeamPoints.ShouldBe(12);
        lower.TeamPoints.ShouldBe(9);
    }

    private static string Path(int meetId, int participationId, Discipline discipline, int round) =>
        $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}";

    private async Task<(int MeetId, string MeetSlug)> CreateMeetAsync(CreateMeetCommandBuilder builder)
    {
        CreateMeetCommand command = builder.Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}",
            CancellationToken.None);

        return (details!.MeetId, slug);
    }

    private async Task RecordAttempt(int participationId, Discipline discipline, int round, decimal weight, bool good)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(good)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(_meetId, participationId, discipline, round),
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task RecordAttemptForMeet(
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight,
        bool good)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(good)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(meetId, participationId, discipline, round),
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<int> AddParticipantAsync()
    {
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().WithCountryId(2).Build();
        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .Build();

        HttpResponseMessage participantResponse = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{_meetId}/participants",
            participantCommand,
            CancellationToken.None);

        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }

    private async Task<int> AddParticipantToMeetAsync(int meetId, decimal bodyWeight = 80.5m)
    {
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().WithCountryId(2).Build();
        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .Build();

        HttpResponseMessage participantResponse = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants",
            participantCommand,
            CancellationToken.None);

        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }
}