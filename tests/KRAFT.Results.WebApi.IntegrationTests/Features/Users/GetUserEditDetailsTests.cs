using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

[Collection(nameof(UsersCollection))]
public sealed class GetUserEditDetailsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string BasePath = "/users";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

    private int _userId;
    private string _email = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateUserCommand createCommand = new CreateUserCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();
        _email = createCommand.Email;
        List<UserSummary>? users = await _authorizedHttpClient.GetFromJsonAsync<List<UserSummary>>(BasePath, CancellationToken.None);
        _userId = users!.First(u => u.Email == _email).UserId;
        ChangeUserRoleCommand roleCommand = new(["Admin"]);
        await _authorizedHttpClient.PatchAsJsonAsync($"{BasePath}/{_userId}/role", roleCommand, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (_userId != 0)
        {
            try
            {
                await _authorizedHttpClient.DeleteAsync($"{BasePath}/{_userId}", CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                // Best-effort cleanup; do not mask test failures.
            }
        }

        _authorizedHttpClient.Dispose();
        _nonAdminHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WithUserDetails_WhenUserExists()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{_userId}/edit", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserEditDetails? details = await response.Content.ReadFromJsonAsync<UserEditDetails>(CancellationToken.None);
        details.ShouldNotBeNull();
        details.Email.ShouldBe(_email);
        details.Roles.ShouldContain("Admin");
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

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.GetAsync(
            $"{BasePath}/{_userId}/edit", CancellationToken.None);

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
}