using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

public sealed class DeleteAthleteTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/athletes";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        int athleteId = await CreateAthleteAsync();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{athleteId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenAthleteDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{int.MaxValue}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsConflict_WhenAthleteHasParticipations()
    {
        // Arrange — the seeded athlete has participations
        int seededAthleteId = 1;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{seededAthleteId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        int athleteId = await CreateAthleteAsync();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.DeleteAsync($"{BasePath}/{athleteId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.DeleteAsync($"{BasePath}/1", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<int> CreateAthleteAsync()
    {
        CreateAthleteCommand command = new CreateAthleteCommandBuilder().Build();
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(BasePath, command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string? location = response.Headers.Location?.ToString();
        location.ShouldNotBeNull();

        return int.Parse(location.TrimStart('/'), System.Globalization.CultureInfo.InvariantCulture);
    }
}