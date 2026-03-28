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
        string slug = await CreateAthleteAsync();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{slug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WithDescription_WhenAthleteDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/non-existent-slug", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        body.ShouldContain("Athlete not found.");
    }

    [Fact]
    public async Task ReturnsConflict_WithDescription_WhenAthleteHasParticipations()
    {
        // Arrange — the seeded athlete has participations
        string seededAthleteSlug = Constants.TestAthleteSlug;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{seededAthleteSlug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        body.ShouldContain("Cannot delete an athlete that has participations.");
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        string slug = await CreateAthleteAsync();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.DeleteAsync($"{BasePath}/{slug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.DeleteAsync($"{BasePath}/some-slug", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> CreateAthleteAsync()
    {
        CreateAthleteCommand createCommand = new CreateAthleteCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        List<AthleteSummary>? athletes = await _authorizedHttpClient.GetFromJsonAsync<List<AthleteSummary>>(BasePath, CancellationToken.None);
        AthleteSummary athlete = athletes!.First(a => a.Name == $"{createCommand.FirstName} {createCommand.LastName}");

        return athlete.Slug!;
    }
}