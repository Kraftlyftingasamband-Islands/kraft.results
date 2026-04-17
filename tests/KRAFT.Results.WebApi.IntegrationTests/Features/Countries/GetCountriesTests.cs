using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Countries;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Countries;

[Collection(nameof(InfraCollection))]
public sealed class GetCountriesTests
{
    private const string Path = "/countries";

    private readonly HttpClient _unauthorizedHttpClient;

    public GetCountriesTests(CollectionFixture fixture)
    {
        _unauthorizedHttpClient = fixture.Factory!.CreateClient();
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
        IReadOnlyList<CountrySummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<CountrySummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestTeam()
    {
        // Arrange

        // Act
        IReadOnlyList<CountrySummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<CountrySummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Name == Constants.TestCountryName);
    }
}