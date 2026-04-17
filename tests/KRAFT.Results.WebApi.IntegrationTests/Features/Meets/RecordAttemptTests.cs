using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(RecordAttemptsCollection))]
public sealed class RecordAttemptTests
{
    private const int SeedMeetId = 1;
    private const int SeedParticipationId = 2;

    private readonly CollectionFixture _fixture;
    private readonly HttpClient _authorizedHttpClient;
    private readonly HttpClient _unauthorizedHttpClient;
    private readonly HttpClient _nonAdminHttpClient;

    public RecordAttemptTests(CollectionFixture fixture)
    {
        _fixture = fixture;
        _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
        _unauthorizedHttpClient = fixture.Factory.CreateClient();
        _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenCreatingNewAttempt()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();
        RecordAttemptCommand command = new RecordAttemptCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNoContent_WhenUpdatingExistingAttempt()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();
        RecordAttemptCommand createCommand = new RecordAttemptCommandBuilder()
            .WithWeight(100.0m)
            .Build();

        await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId, Discipline.Squat, 1),
            createCommand,
            CancellationToken.None);

        RecordAttemptCommand updateCommand = new RecordAttemptCommandBuilder()
            .WithWeight(110.0m)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId, Discipline.Squat, 1),
            updateCommand,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TotalsRecalculated_AfterAttemptRecorded()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();

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
                $"/meets/{Constants.TestMeetSlug}/participations",
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
            Path(SeedMeetId, 99999, Discipline.Squat, 1),
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
            Path(99999, SeedParticipationId, Discipline.Squat, 1),
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
            Path(SeedMeetId, SeedParticipationId, Discipline.Squat, 1),
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
            Path(SeedMeetId, SeedParticipationId, Discipline.Squat, 1),
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
            Path(SeedMeetId, SeedParticipationId, (Discipline)99, 1),
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
            Path(SeedMeetId, SeedParticipationId, Discipline.Squat, 4),
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
            Path(SeedMeetId, SeedParticipationId, Discipline.Squat, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BombedOut_WhenNoBenchGoodLifts()
    {
        // Arrange - all bench attempts are no-good
        int participationId = await AddParticipantToSeedMeet();

        await RecordAttempt(participationId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttempt(participationId, Discipline.Bench, 1, 60.0m, false);
        await RecordAttempt(participationId, Discipline.Bench, 2, 60.0m, false);
        await RecordAttempt(participationId, Discipline.Bench, 3, 60.0m, false);
        await RecordAttempt(participationId, Discipline.Deadlift, 1, 150.0m, true);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{Constants.TestMeetSlug}/participations",
                CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation? bombedOut = participations
            .LastOrDefault(p => p.Total == 0m && p.Attempts.Any());

        bombedOut.ShouldNotBeNull();
    }

    [Fact]
    public async Task AttemptPersistedThroughAggregate_WhenRecordedViaParticipation()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();

        // Act
        await RecordAttempt(participationId, Discipline.Squat, 1, 120.0m, true);

        // Assert — retrieve participation and verify attempt is persisted and retrievable
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{Constants.TestMeetSlug}/participations",
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
        int participationId = await AddParticipantToSeedMeet();

        await RecordAttempt(participationId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttempt(participationId, Discipline.Squat, 2, 110.0m, true);

        DbContextOptions<ResultsDbContext> dbOptions = new DbContextOptionsBuilder<ResultsDbContext>()
            .UseSqlServer(_fixture.Database.ConnectionString)
            .Options;

        await using (ResultsDbContext dbContext = new(dbOptions))
        {
            await dbContext.Database.ExecuteSqlAsync(
                $"INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy) VALUES ({participationId}, 1, 4, 0, 1, 'seed', 'seed')",
                TestContext.Current.CancellationToken);
        }

        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(115.0m)
            .Build();

        // Act — editing round 3 must not be blocked by the legacy round 4 entry
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId, Discipline.Squat, 3),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static string Path(int meetId, int participationId, Discipline discipline, int round) =>
        $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}";

    private async Task RecordAttempt(int participationId, Discipline discipline, int round, decimal weight, bool good)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(good)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId, discipline, round),
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<int> AddParticipantToSeedMeet()
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
            $"/meets/{SeedMeetId}/participants",
            participantCommand,
            CancellationToken.None);

        AddParticipantResponse? result = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }
}