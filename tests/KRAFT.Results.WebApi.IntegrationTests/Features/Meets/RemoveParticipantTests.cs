using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class RemoveParticipantTests(IntegrationTestFixture fixture)
{
    private const int NonExistentMeetId = 99999;
    private const int NonExistentParticipationId = 99999;
    private const int ExistingMeetId = 2;
    private const int ExistingWeightCategoryId = 1;

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        int participationId = await AddParticipantAsync(ExistingMeetId);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{ExistingMeetId}/participants/{participationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{ExistingMeetId}/participants/{NonExistentParticipationId}", CancellationToken.None);

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
            $"/meets/{ExistingMeetId}/participants/1", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.DeleteAsync(
            $"/meets/{ExistingMeetId}/participants/1", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<int> AddParticipantAsync(int meetId)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteId(1)
            .WithWeightCategoryId(ExistingWeightCategoryId)
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