using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

[Collection(nameof(TeamsCollection))]
public sealed class GetTeamsTests(CollectionFixture fixture)
{
    private const string Path = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

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

    [Fact]
    public async Task OrdersTeamsByTitle()
    {
        // Arrange
        CreateTeamCommand command = new CreateTeamCommandBuilder()
            .WithTitle("0")
            .Build();
        await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Act
        IReadOnlyList<TeamSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<TeamSummary>>(Path, CancellationToken.None);

        // Assert
        response![0].Title.ShouldBe(command.Title);
    }
}