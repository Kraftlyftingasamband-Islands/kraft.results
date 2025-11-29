using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

public sealed class GetAthletesTests : IClassFixture<IntegrationTestFixture>
{
    private const string Root = "/athletes";

    private readonly HttpClient _httpClient;

    public GetAthletesTests(IntegrationTestFixture fixture)
    {
        _httpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync(Root, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _httpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Root, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }
}