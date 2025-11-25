using System.Net;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class HealthCheckTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _httpClient;

    public HealthCheckTests(IntegrationTestFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _httpClient = factory.CreateClient();
    }

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