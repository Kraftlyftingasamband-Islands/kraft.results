using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

[Collection(nameof(AthletesCollection))]
public sealed class GetAthleteDetailsTests
{
    private const string BasePath = "/athletes";

    private readonly HttpClient _unauthorizedHttpClient;

    public GetAthleteDetailsTests(CollectionFixture fixture)
    {
        _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    }

    [Fact]
    public async Task ReturnsOk_WhenAthleteExists()
    {
        // Arrange
        string path = $"{BasePath}/{Constants.TestAthleteSlug}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange
        string path = $"{BasePath}/{Constants.TestAthleteSlug}";

        // Act
        AthleteDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<AthleteDetails>(path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsCorrectAthlete()
    {
        // Arrange
        string path = $"{BasePath}/{Constants.TestAthleteSlug}";

        // Act
        AthleteDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<AthleteDetails>(path, CancellationToken.None);

        // Assert
        response!.Slug.ShouldBe(Constants.TestAthleteSlug);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenAthleteDoesNotExist()
    {
        // Arrange
        string path = $"{BasePath}/{Guid.NewGuid}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}