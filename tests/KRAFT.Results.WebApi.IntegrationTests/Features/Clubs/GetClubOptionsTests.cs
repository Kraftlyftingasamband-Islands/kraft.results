using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Clubs;

[Collection(nameof(ClubsCollection))]
public sealed class GetClubOptionsTests(CollectionFixture fixture)
{
    private const string Path = "/teams/options";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<ClubOption>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<ClubOption>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTeamsWithIdAndTitle()
    {
        // Arrange

        // Act
        IReadOnlyList<ClubOption>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<ClubOption>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Id > 0 && !string.IsNullOrEmpty(x.Title));
    }

    [Fact]
    public async Task OrdersByTitle()
    {
        // Arrange

        // Act
        IReadOnlyList<ClubOption>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<ClubOption>>(Path, CancellationToken.None);

        // Assert
        List<string> titles = response!.Select(x => x.Title).ToList();
        titles.ShouldBe(titles.OrderBy(x => x).ToList());
    }
}
