using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

[Collection(nameof(TeamsCollection))]
public sealed class GetTeamsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private string _slug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateClubCommand command = new CreateClubCommandBuilder()
            .WithTitle("0")
            .Build();
        await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        List<ClubSummary>? teams = await _authorizedHttpClient.GetFromJsonAsync<List<ClubSummary>>(Path, CancellationToken.None);
        ClubSummary team = teams!.First(t => t.ShortTitle == command.TitleShort);
        _slug = team.Slug;
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_slug))
        {
            try
            {
                await _authorizedHttpClient.DeleteAsync($"/teams/{_slug}", CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                // Best-effort cleanup; do not mask test failures.
            }
        }

        _authorizedHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<ClubSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<ClubSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestTeam()
    {
        // Arrange

        // Act
        IReadOnlyList<ClubSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<ClubSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Slug == _slug);
    }

    [Fact]
    public async Task OrdersTeamsByTitle()
    {
        // Arrange

        // Act
        IReadOnlyList<ClubSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<ClubSummary>>(Path, CancellationToken.None);

        // Assert
        response![0].Title.ShouldBe("0");
    }
}
