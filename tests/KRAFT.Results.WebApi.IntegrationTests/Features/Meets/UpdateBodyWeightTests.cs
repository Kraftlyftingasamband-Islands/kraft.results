using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class UpdateBodyWeightTests
{
    private const int SeedMeetId = 1;

    private readonly HttpClient _authorizedHttpClient;
    private readonly HttpClient _unauthorizedHttpClient;
    private readonly HttpClient _nonAdminHttpClient;

    public UpdateBodyWeightTests(IntegrationTestFixture fixture)
    {
        _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
        _unauthorizedHttpClient = fixture.Factory.CreateClient();
        _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(SeedMeetId, participationId),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdatesBodyWeight_WhenSuccessful()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();
        decimal newWeight = 92.75m;
        UpdateBodyWeightCommand command = new(newWeight);

        // Act
        await _authorizedHttpClient.PatchAsJsonAsync(
            Path(SeedMeetId, participationId),
            command,
            CancellationToken.None);

        // Assert
        List<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<List<MeetParticipation>>(
                $"/meets/{Constants.TestMeetSlug}/participations",
                CancellationToken.None);

        participations.ShouldNotBeNull();
        participations.ShouldContain(p => p.ParticipationId == participationId && p.BodyWeight == newWeight);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationDoesNotExist()
    {
        // Arrange
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
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
        UpdateBodyWeightCommand command = new(85.50m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(99999, 1),
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
            Path(SeedMeetId, 1),
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
            Path(SeedMeetId, 1),
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
            Path(SeedMeetId, 1),
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
            Path(SeedMeetId, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsNoContent_WhenBodyWeightIsAtMaximum()
    {
        // Arrange
        int participationId = await AddParticipantToSeedMeet();
        UpdateBodyWeightCommand command = new(400.0m);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            Path(SeedMeetId, participationId),
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
            Path(SeedMeetId, 1),
            command,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static string Path(int meetId, int participationId) =>
        $"/meets/{meetId}/participations/{participationId}";

    private async Task<int> AddParticipantToSeedMeet()
    {
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().WithCountryId(2).Build();
        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = ValueObjects.Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

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