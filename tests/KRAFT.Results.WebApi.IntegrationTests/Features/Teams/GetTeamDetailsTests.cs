using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

[Collection(nameof(TeamsCollection))]
public sealed class GetTeamDetailsTests
{
    private const string BasePath = "/teams";

    private readonly HttpClient _unauthorizedHttpClient;

    public GetTeamDetailsTests(CollectionFixture fixture)
    {
        _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    }

    [Fact]
    public async Task ReturnsOk_WhenTeamExists()
    {
        // Arrange
        string path = $"{BasePath}/{Constants.TestTeamSlug}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange
        string path = $"{BasePath}/{Constants.TestTeamSlug}";

        // Act
        TeamDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<TeamDetails>(path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsCorrectTeam()
    {
        // Arrange
        string path = $"{BasePath}/{Constants.TestTeamSlug}";

        // Act
        TeamDetails? response = await _unauthorizedHttpClient.GetFromJsonAsync<TeamDetails>(path, CancellationToken.None);

        // Assert
        response!.Slug.ShouldBe(Constants.TestTeamSlug);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenTeamDoesNotExist()
    {
        // Arrange
        string path = $"{BasePath}/{Guid.NewGuid}";

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}