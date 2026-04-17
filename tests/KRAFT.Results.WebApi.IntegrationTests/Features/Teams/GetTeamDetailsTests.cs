using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

[Collection(nameof(TeamsCollection))]
public sealed class GetTeamDetailsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string BasePath = "/teams";

    private readonly HttpClient _authorizedClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private string _slug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateTeamCommand command = new CreateTeamCommandBuilder().Build();
        await _authorizedClient.PostAsJsonAsync(BasePath, command, CancellationToken.None);

        List<TeamSummary>? teams = await _authorizedClient.GetFromJsonAsync<List<TeamSummary>>(BasePath, CancellationToken.None);
        TeamSummary team = teams!.First(t => t.ShortTitle == command.TitleShort);
        _slug = team.Slug;
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_slug))
        {
            try
            {
                await _authorizedClient.DeleteAsync($"/teams/{_slug}", CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                // Best-effort cleanup; do not mask test failures.
            }
        }

        _authorizedClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WhenTeamExists()
    {
        // Arrange
        string path = $"{BasePath}/{_slug}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange
        string path = $"{BasePath}/{_slug}";

        // Act
        TeamDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<TeamDetails>(path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsCorrectTeam()
    {
        // Arrange
        string path = $"{BasePath}/{_slug}";

        // Act
        TeamDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<TeamDetails>(path, CancellationToken.None);

        // Assert
        response!.Slug.ShouldBe(_slug);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenTeamDoesNotExist()
    {
        // Arrange
        string path = $"{BasePath}/{Guid.NewGuid}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}