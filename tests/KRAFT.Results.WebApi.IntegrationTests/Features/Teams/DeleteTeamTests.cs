using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

public sealed class DeleteTeamTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        string slug = await CreateTeamAsync();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{slug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenTeamDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/non-existent-slug", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsConflict_WhenTeamHasAthletes()
    {
        // Arrange
        (string slug, int teamId) = await CreateTeamWithIdAsync();

        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithTeamId(teamId)
            .Build();
        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync("/athletes", athleteCommand, CancellationToken.None);
        athleteResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{slug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        string slug = await CreateTeamAsync();

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

    private async Task<string> CreateTeamAsync()
    {
        (string slug, _) = await CreateTeamWithIdAsync();
        return slug;
    }

    private async Task<(string Slug, int TeamId)> CreateTeamWithIdAsync()
    {
        CreateTeamCommand createCommand = new CreateTeamCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        string? location = createResponse.Headers.Location?.ToString();
        location.ShouldNotBeNull();
        int teamId = int.Parse(location.TrimStart('/'), System.Globalization.CultureInfo.InvariantCulture);

        List<TeamSummary>? teams = await _authorizedHttpClient.GetFromJsonAsync<List<TeamSummary>>(BasePath, CancellationToken.None);
        TeamSummary team = teams!.First(t => t.ShortTitle == createCommand.TitleShort);

        return (team.Slug, teamId);
    }
}