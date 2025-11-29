using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Teams;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

public sealed class GetTeamsTests : IClassFixture<IntegrationTestFixture>
{
    private const string Path = "/teams";

    private readonly HttpClient _unauthorizedHttpClient;

    public GetTeamsTests(IntegrationTestFixture fixture)
    {
        _unauthorizedHttpClient = fixture.Factory.CreateClient();
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
        IReadOnlyList<TeamSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<TeamSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestTeam()
    {
        // Arrange

        // Act
        IReadOnlyList<TeamSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<TeamSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Slug == Constants.TestTeamSlug);
    }
}