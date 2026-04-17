using System.Net;
using System.Net.Http.Json;

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

    private static string Path(int meetId, int participationId) =>
        $"/meets/{meetId}/participations/{participationId}";
}