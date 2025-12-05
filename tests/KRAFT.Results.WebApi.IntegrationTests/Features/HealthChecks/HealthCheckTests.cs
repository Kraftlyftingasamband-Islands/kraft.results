using System.Net;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.HealthChecks;

public sealed class HealthCheckTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task HealthCheckReturnsHealthyStatus()
    {
        // Arrange
        using HttpRequestMessage request = new(HttpMethod.Get, "/healthz");

        // Act
        HttpResponseMessage response = await _httpClient.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}