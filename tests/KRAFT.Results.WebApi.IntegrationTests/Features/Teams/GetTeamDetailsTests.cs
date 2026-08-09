using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Clubs;
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
        CreateClubCommand command = new CreateClubCommandBuilder().Build();
        await _authorizedClient.PostAsJsonAsync(BasePath, command, CancellationToken.None);

        List<ClubSummary>? teams = await _authorizedClient.GetFromJsonAsync<List<ClubSummary>>(BasePath, CancellationToken.None);
        ClubSummary team = teams!.First(t => t.ShortTitle == command.TitleShort);
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
        ClubDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<ClubDetails>(path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsCorrectTeam()
    {
        // Arrange
        string path = $"{BasePath}/{_slug}";

        // Act
        ClubDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<ClubDetails>(path, CancellationToken.None);

        // Assert
        response!.Slug.ShouldBe(_slug);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenTeamDoesNotExist()
    {
        // Arrange
        string path = $"{BasePath}/{Guid.NewGuid()}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsParticipationCount_WhenTeamHasMembers()
    {
        // Arrange
        // Uses the shared seed team which has a pre-seeded athlete with participations
        string path = $"{BasePath}/{Constants.TestTeamSlug}";

        // Act
        ClubDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<ClubDetails>(path, CancellationToken.None);

        // Assert
        ClubDetails details = response.ShouldNotBeNull();
        ClubMember member = details.Members.Single(m => m.Slug == Constants.TestAthleteSlug);
        member.ParticipationCount.ShouldBe(3); // BaseSeedSql seeds 3 participations for the test athlete
    }
}
