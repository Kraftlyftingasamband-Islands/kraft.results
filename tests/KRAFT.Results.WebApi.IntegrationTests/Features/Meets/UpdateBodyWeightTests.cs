using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class UpdateBodyWeightTests(CollectionFixture fixture) : IAsyncLifetime
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

        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().WithCountryCode("NOR").Build();
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

        _participationId = result!.ParticipationId;
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
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdatesBodyWeight_WhenSuccessful()
    {
        // Arrange
        decimal newWeight = 92.75m;
        UpdateBodyWeightCommand command = new(newWeight);

        // Act
        await _authorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        List<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<List<MeetParticipation>>(
                $"/meets/{_meetSlug}/participations",
                CancellationToken.None);

        participations.ShouldNotBeNull();
        participations.ShouldContain(p => p.ParticipationId == _participationId && p.BodyWeight == newWeight);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationDoesNotExist()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, 99999),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(99999, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenBodyWeightIsZero()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(0m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenBodyWeightIsNegative()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(-1m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenBodyWeightExceedsMaximum()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(401m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsNoContent_WhenBodyWeightIsAtMaximum()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(400.0m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenNotAdmin()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PatchAsJsonAsync(
            Path(_meetId, _participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateBodyWeight_SwapsTiebreaker_WhenLighterParticipantBecomesHeavier()
    {
        // Arrange — two participants with the same total; lighter (75.0kg) is initially rank 1
        (int meetId, string meetSlug) = await CreateMeetAsync(new CreateMeetCommandBuilder());

        (int lighterParticipationId, string lighterAthleteSlug) = await AddParticipantToMeetAsync(meetId, bodyWeight: 75.0m);
        (int heavierParticipationId, string heavierAthleteSlug) = await AddParticipantToMeetAsync(meetId, bodyWeight: 80.0m);

        // Both record the same total (240 kg)
        await RecordAttemptForMeet(meetId, lighterParticipationId, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, lighterParticipationId, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, lighterParticipationId, Discipline.Deadlift, 1, 120.0m, true);

        await RecordAttemptForMeet(meetId, heavierParticipationId, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, heavierParticipationId, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, heavierParticipationId, Discipline.Deadlift, 1, 120.0m, true);

        // Act — update the lighter participant's body weight to 85.0kg (now heavier than the other)
        UpdateBodyWeightCommand command = new(85.0m);
        HttpResponseMessage updateResponse = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(meetId, lighterParticipationId),
            command,
            CancellationToken.None);

        updateResponse.EnsureSuccessStatusCode();

        // Assert — places must have swapped: heavier-at-registration (80.0kg) is now rank 1
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{meetSlug}/participations",
                CancellationToken.None);

        participations.ShouldNotBeNull();
        MeetParticipation? formerlyLighter = participations.FirstOrDefault(p => p.ParticipationId == lighterParticipationId);
        MeetParticipation? formerlyHeavier = participations.FirstOrDefault(p => p.ParticipationId == heavierParticipationId);

        formerlyLighter.ShouldNotBeNull();
        formerlyHeavier.ShouldNotBeNull();
        formerlyLighter.Rank.ShouldBe(2, "formerly lighter participant is now 85.0kg — should be rank 2");
        formerlyHeavier.Rank.ShouldBe(1, "formerly heavier participant is now the lightest — should be rank 1");

        // Cleanup — reverse FK order: participants → meet → athletes
        (await _authorizedHttpClient.DeleteAsync($"/meets/{meetId}/participants/{lighterParticipationId}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/meets/{meetId}/participants/{heavierParticipationId}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/meets/{meetSlug}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/athletes/{lighterAthleteSlug}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/athletes/{heavierAthleteSlug}", CancellationToken.None)).EnsureSuccessStatusCode();
    }

    private static string Path(int meetId, int participationId) =>
        $"/meets/{meetId}/participations/{participationId}";

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

    private async Task<(int ParticipationId, string AthleteSlug)> AddParticipantToMeetAsync(int meetId, decimal bodyWeight = 80.5m)
    {
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().WithCountryCode("NOR").Build();
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

        return (result!.ParticipationId, athleteSlug);
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
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}