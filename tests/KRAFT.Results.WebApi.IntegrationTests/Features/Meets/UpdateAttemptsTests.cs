using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class UpdateAttemptsTests
{
    private const int SeedMeetId = 1;
    private const int SeedParticipationId = 2;

    private readonly HttpClient _authorizedHttpClient;
    private readonly HttpClient _unauthorizedHttpClient;
    private readonly HttpClient _nonAdminHttpClient;

    public UpdateAttemptsTests(IntegrationTestFixture fixture)
    {
        _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
        _unauthorizedHttpClient = fixture.Factory.CreateClient();
        _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenNotAdmin()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationDoesNotExist()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, 99999),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(99999, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationBelongsToDifferentMeet()
    {
        // Arrange - participation 2 belongs to meet 1, not meet 2
        int differentMeetId = 2;
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(differentMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDisciplineIsInvalid()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts([new(Discipline.None, 1, 100.0m, true)])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenRoundIsInvalid()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts([new(Discipline.Squat, 4, 100.0m, true)])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenRoundIsZero()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts([new(Discipline.Squat, 0, 100.0m, true)])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenWeightIsZero()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts([new(Discipline.Squat, 1, 0m, true)])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenWeightIsNegative()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts([new(Discipline.Squat, 1, -10.0m, true)])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDuplicateDisciplineRound()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts(
            [
                new(Discipline.Squat, 1, 100.0m, true),
                new(Discipline.Squat, 1, 110.0m, false),
            ])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTooManyAttempts()
    {
        // Arrange
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts(
            [
                new(Discipline.Squat, 1, 100.0m, true),
                new(Discipline.Squat, 2, 110.0m, true),
                new(Discipline.Squat, 3, 120.0m, true),
                new(Discipline.Bench, 1, 60.0m, true),
                new(Discipline.Bench, 2, 65.0m, true),
                new(Discipline.Bench, 3, 70.0m, true),
                new(Discipline.Deadlift, 1, 140.0m, true),
                new(Discipline.Deadlift, 2, 150.0m, true),
                new(Discipline.Deadlift, 3, 160.0m, true),
                new(Discipline.Squat, 1, 100.0m, true),
            ])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, SeedParticipationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecalculatesTotals_WithBestGoodLifts()
    {
        // Arrange - add a fresh participant to seed meet
        int participationId = await AddParticipantToSeedMeet();

        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts(
            [
                new(Discipline.Squat, 1, 100.0m, true),
                new(Discipline.Squat, 2, 110.0m, true),
                new(Discipline.Squat, 3, 120.0m, false),
                new(Discipline.Bench, 1, 60.0m, true),
                new(Discipline.Bench, 2, 70.0m, true),
                new(Discipline.Deadlift, 1, 150.0m, true),
                new(Discipline.Deadlift, 2, 160.0m, true),
            ])
            .Build();

        // Act
        await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId),
            command,
            CancellationToken.None);

        // Assert - best good lifts: Squat=110, Bench=70, Deadlift=160, Total=340
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{Constants.TestMeetSlug}/participations",
                CancellationToken.None);

        participations.ShouldNotBeNull();
        participations.ShouldContain(p => p.Total == 340.0m);
    }

    [Fact]
    public async Task RecalculatesTotals_BombedOut_WhenDisciplineHasNoGoodLifts()
    {
        // Arrange - all bench attempts are no-good
        int participationId = await AddParticipantToSeedMeet();

        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts(
            [
                new(Discipline.Squat, 1, 100.0m, true),
                new(Discipline.Bench, 1, 60.0m, false),
                new(Discipline.Bench, 2, 60.0m, false),
                new(Discipline.Bench, 3, 60.0m, false),
                new(Discipline.Deadlift, 1, 150.0m, true),
            ])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{Constants.TestMeetSlug}/participations",
                CancellationToken.None);

        // The bombed-out participant will have Total=0
        // Find it by looking at the last entries (bombed out sorts to bottom)
        MeetParticipation? bombedOut = participations!
            .LastOrDefault(p => p.Total == 0m && p.Attempts.Any());

        bombedOut.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenEmptyAttemptsList()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts([])
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task IsIdempotent_WhenCalledTwiceWithSameData()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder()
            .WithAttempts(
            [
                new(Discipline.Squat, 1, 100.0m, true),
                new(Discipline.Bench, 1, 60.0m, true),
                new(Discipline.Deadlift, 1, 140.0m, true),
            ])
            .Build();

        // Act
        await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId),
            command,
            CancellationToken.None);

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PreservesRecords_WhenReplacingAttemptsWithLinkedRecords()
    {
        // Arrange - participation 1 has attempts with linked records
        int participationWithRecords = 1;
        UpdateAttemptsCommand command = new UpdateAttemptsCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            Path(SeedMeetId, participationWithRecords),
            command,
            CancellationToken.None);

        // Assert - should succeed without FK errors (records get AttemptId set to NULL)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static string Path(int meetId, int participationId) =>
        $"/meets/{meetId}/participants/{participationId}/attempts";

    private async Task<int> AddParticipantToSeedMeet()
    {
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().Build();
        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        string athleteLocation = athleteResponse.Headers.Location!.ToString();
        string[] segments = athleteLocation.Split('/');
        int athleteId = int.Parse(segments[^1], CultureInfo.InvariantCulture);

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteId(athleteId)
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