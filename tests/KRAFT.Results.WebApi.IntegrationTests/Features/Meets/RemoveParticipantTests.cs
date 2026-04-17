using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class RemoveParticipantTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const int NonExistentMeetId = 99999;
    private const int NonExistentParticipationId = 99999;
    private const int IrrelevantParticipationId = int.MaxValue;

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private int _meetId;
    private string _meetSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder().Build();

        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync("/meets", command, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        _meetSlug = createResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meetSlug}", CancellationToken.None);
        _meetId = details!.MeetId;
    }

    public async ValueTask DisposeAsync()
    {
        if (_meetId != 0)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{_meetSlug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _nonAdminHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        int participationId = await AddParticipantAsync(_meetId);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{participationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{NonExistentParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.ParticipationNotFound");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{NonExistentMeetId}/participants/{NonExistentParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{IrrelevantParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{IrrelevantParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<int> AddParticipantAsync(int meetId)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(82.5m)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        AddParticipantResponse? body = await response.Content.ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);
        body.ShouldNotBeNull();

        return body.ParticipationId;
    }
}