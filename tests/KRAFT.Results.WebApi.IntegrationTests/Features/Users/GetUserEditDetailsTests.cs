using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Users;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class GetUserEditDetailsTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/users";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WithUserDetails_WhenUserExists()
    {
        // Arrange
        int userId = await GetSeededUserIdAsync();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{userId}/edit", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserEditDetails? details = await response.Content.ReadFromJsonAsync<UserEditDetails>(CancellationToken.None);
        details.ShouldNotBeNull();
        details.Email.ShouldBe(Constants.TestUser.Email);
        details.Role.ShouldBe("Admin");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{int.MaxValue}/edit", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        int userId = await GetSeededUserIdAsync();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.GetAsync(
            $"{BasePath}/{userId}/edit", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/1/edit", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<int> GetSeededUserIdAsync()
    {
        List<UserSummary>? users = await _authorizedHttpClient.GetFromJsonAsync<List<UserSummary>>(BasePath, CancellationToken.None);
        UserSummary user = users!.First(u => u.Email == Constants.TestUser.Email);
        return user.UserId;
    }
}