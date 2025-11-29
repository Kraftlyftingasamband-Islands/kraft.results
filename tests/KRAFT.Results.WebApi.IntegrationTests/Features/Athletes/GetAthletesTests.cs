using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

public sealed class GetAthletesTests : IClassFixture<IntegrationTestFixture>
{
    private const string Path = "/athletes";

    private readonly HttpClient _unauthorizedHttpClient;

    public GetAthletesTests(IntegrationTestFixture fixture)
    {
        _unauthorizedHttpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        var response = await _unauthorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }
}