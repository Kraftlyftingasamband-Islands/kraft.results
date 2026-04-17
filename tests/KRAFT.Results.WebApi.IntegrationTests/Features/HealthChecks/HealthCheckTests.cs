using System.Net;

using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.HealthChecks;

[Collection(nameof(InfraCollection))]
public sealed class HealthCheckTests(CollectionFixture fixture)
{
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();

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