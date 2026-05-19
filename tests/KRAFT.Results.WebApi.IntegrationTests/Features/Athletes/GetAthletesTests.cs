using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

[Collection(nameof(AthletesCollection))]
public sealed class GetAthletesTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/athletes";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private string _slug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName("0")
            .WithLastName("0")
            .Build();
        await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        List<AthleteSummary>? athletes = await _authorizedHttpClient.GetFromJsonAsync<List<AthleteSummary>>(Path, CancellationToken.None);
        AthleteSummary athlete = athletes!.First(a => a.Name == "0 0");
        _slug = athlete.Slug!;
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_slug))
        {
            try
            {
                await _authorizedHttpClient.DeleteAsync($"/athletes/{_slug}", CancellationToken.None);
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
    public async Task ReturnsOk_WhenNotAuthenticated()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestAthlete()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Slug == _slug);
    }

    [Fact]
    public async Task ReturnsTestAthletesOrderedByFirstName()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        response![0].Name.ShouldBe("0 0");
    }

    [Fact]
    public async Task SeededAthlete_ReturnsParticipationCount()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        AthleteSummary? seededAthlete = response!.FirstOrDefault(x => x.Slug == TestSeedConstants.Athlete.Slug);
        seededAthlete.ShouldNotBeNull();
        seededAthlete.ParticipationCount.ShouldBe(3);
    }

    [Fact]
    public async Task SeededAthlete_ReturnsTeam()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        AthleteSummary? seededAthlete = response!.FirstOrDefault(x => x.Slug == TestSeedConstants.Athlete.Slug);
        seededAthlete.ShouldNotBeNull();
        seededAthlete.Team.ShouldBe(TestSeedConstants.Team.Title);
    }

    [Fact]
    public async Task NewAthlete_ReturnsZeroParticipationCount()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        AthleteSummary? newAthlete = response!.FirstOrDefault(x => x.Slug == _slug);
        newAthlete.ShouldNotBeNull();
        newAthlete.ParticipationCount.ShouldBe(0);
    }

    [Fact]
    public async Task NewAthlete_ReturnsNullTeam()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        AthleteSummary? newAthlete = response!.FirstOrDefault(x => x.Slug == _slug);
        newAthlete.ShouldNotBeNull();
        newAthlete.Team.ShouldBeNull();
    }
}