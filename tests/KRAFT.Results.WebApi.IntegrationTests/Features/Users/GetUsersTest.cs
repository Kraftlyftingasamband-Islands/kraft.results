using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

[Collection(nameof(UsersCollection))]
public sealed class GetUsersTest
{
    private const string Path = "/users";

    private readonly HttpClient _authorizedHttpClient;
    private readonly HttpClient _nonAdminHttpClient;
    private readonly HttpClient _unauthorizedHttpClient;

    public GetUsersTest(CollectionFixture fixture)
    {
        _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
        _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
        _unauthorizedHttpClient = fixture.Factory!.CreateClient();
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
        IReadOnlyList<UserSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<UserSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestUser()
    {
        // Arrange

        // Act
        IReadOnlyList<UserSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<UserSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Email == Constants.TestUser.Email);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenHttpClientIsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}