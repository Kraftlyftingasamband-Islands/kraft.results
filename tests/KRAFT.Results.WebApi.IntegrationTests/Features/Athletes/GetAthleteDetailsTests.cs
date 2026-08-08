using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

[Collection(nameof(AthletesCollection))]
public sealed class GetAthleteDetailsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string BasePath = "/athletes";

    private readonly HttpClient _authorizedClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private string _slug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateAthleteCommand command = new CreateAthleteCommandBuilder().Build();
        await _authorizedClient.PostAsJsonAsync(BasePath, command, CancellationToken.None);

        List<AthleteSummary>? athletes = await _authorizedClient.GetFromJsonAsync<List<AthleteSummary>>(BasePath, CancellationToken.None);
        AthleteSummary athlete = athletes!.First(a => a.Name == $"{command.FirstName} {command.LastName}");
        _slug = athlete.Slug!;
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_slug))
        {
            try
            {
                await _authorizedClient.DeleteAsync($"/athletes/{_slug}", CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                // Best-effort cleanup; do not mask test failures.
            }
        }

        // Reset the logo that was seeded via SQL — no write endpoint exists for LogoImageFilename
        await fixture.ExecuteSqlAsync(
            $"UPDATE Teams SET LogoImageFilename = NULL WHERE TeamId = {TestSeedConstants.Team.Id}");

        _authorizedClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WhenAthleteExists()
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
        AthleteDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<AthleteDetails>(path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsCorrectAthlete()
    {
        // Arrange
        string path = $"{BasePath}/{_slug}";

        // Act
        AthleteDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<AthleteDetails>(path, CancellationToken.None);

        // Assert
        response!.Slug.ShouldBe(_slug);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenAthleteDoesNotExist()
    {
        // Arrange
        string path = $"{BasePath}/{Guid.NewGuid()}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenTeamHasLogo_ProjectsClubShortTitleAndClubLogoImageFilename()
    {
        // Arrange — seed a logo filename onto the seeded team via SQL (no write endpoint for LogoImageFilename)
        const string LogoFilename = "team-logo.png";

        await fixture.ExecuteSqlAsync(
            $"UPDATE Teams SET LogoImageFilename = {LogoFilename} WHERE TeamId = {TestSeedConstants.Team.Id}");

        string path = $"{BasePath}/{TestSeedConstants.Athlete.Slug}";

        // Act
        AthleteDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<AthleteDetails>(path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.ShouldSatisfyAllConditions(
            () => response.Club.ShouldBe(TestSeedConstants.Team.TitleFull),
            () => response.ClubShortTitle.ShouldBe(TestSeedConstants.Team.TitleShort),
            () => response.ClubLogoImageFilename.ShouldBe(LogoFilename));
    }

    [Fact]
    public async Task WhenTeamHasNoLogo_ClubLogoImageFilenameIsNull()
    {
        // Arrange — the seeded team has no logo by default (LogoImageFilename is NULL in seed)
        string path = $"{BasePath}/{TestSeedConstants.Athlete.Slug}";

        // Act
        AthleteDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<AthleteDetails>(path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.ShouldSatisfyAllConditions(
            () => response.ClubShortTitle.ShouldBe(TestSeedConstants.Team.TitleShort),
            () => response.ClubLogoImageFilename.ShouldBeNull());
    }
}
