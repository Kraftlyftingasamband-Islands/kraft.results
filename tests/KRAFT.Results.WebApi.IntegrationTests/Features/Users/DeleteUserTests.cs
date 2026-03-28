using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class DeleteUserTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/users";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        int userId = await CreateUserAsync();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"{BasePath}/{userId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WithErrorCode_WhenUserDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"{BasePath}/{int.MaxValue}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task ReturnsConflict_WhenDeletingSelf()
    {
        // Arrange — the test auth handler uses the seeded test user
        int selfUserId = await GetSeededUserIdAsync();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"{BasePath}/{selfUserId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.CannotDeleteSelf");
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        int userId = await CreateUserAsync();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.DeleteAsync(
            $"{BasePath}/{userId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.DeleteAsync(
            $"{BasePath}/1", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<int> CreateUserAsync()
    {
        CreateUserCommand createCommand = new CreateUserCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        List<UserSummary>? users = await _authorizedHttpClient.GetFromJsonAsync<List<UserSummary>>(BasePath, CancellationToken.None);
        UserSummary user = users!.First(u => u.Email == createCommand.Email);

        return user.UserId;
    }

    private async Task<int> GetSeededUserIdAsync()
    {
        List<UserSummary>? users = await _authorizedHttpClient.GetFromJsonAsync<List<UserSummary>>(BasePath, CancellationToken.None);
        UserSummary user = users!.First(u => u.Email == Constants.TestUser.Email);
        return user.UserId;
    }
}