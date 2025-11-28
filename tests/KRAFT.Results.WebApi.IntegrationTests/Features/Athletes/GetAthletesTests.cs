using System.Net;

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
}