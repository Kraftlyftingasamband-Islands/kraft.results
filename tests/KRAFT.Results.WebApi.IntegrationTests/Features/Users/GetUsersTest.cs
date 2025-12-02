using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Users;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class GetUsersTest
{
    private const string Path = "/users";

    private readonly HttpClient _unauthorizedHttpClient;

    public GetUsersTest(IntegrationTestFixture fixture)
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
        IReadOnlyList<UserSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<UserSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestUser()
    {
        // Arrange

        // Act
        IReadOnlyList<UserSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<UserSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Email == Constants.TestUser.Email);
    }
}