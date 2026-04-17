using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Eras;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Eras;

[Collection(nameof(InfraCollection))]
public sealed class GetErasTests(CollectionFixture fixture)
{
    private const string Path = "/eras";

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            Path,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsAllEras()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            Path,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        eras.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ReturnsErasOrderedByStartDate()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            Path,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        eras[0].Slug.ShouldBe("historical-era");
        eras[1].Slug.ShouldBe("current-era");
    }

    [Fact]
    public async Task ReturnsEraSummaryWithAllFields()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            Path,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        EraSummary historicalEra = eras.First(e => e.Slug == "historical-era");
        historicalEra.Title.ShouldBe("Historical Era");
        historicalEra.StartDate.ShouldBe(new DateOnly(2011, 1, 1));
        historicalEra.EndDate.ShouldBe(new DateOnly(2018, 12, 31));
    }
}