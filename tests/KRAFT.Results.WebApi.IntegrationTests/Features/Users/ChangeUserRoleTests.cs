using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

[Collection(nameof(UsersCollection))]
public sealed class ChangeUserRoleTests(CollectionFixture fixture)
{
    private const string BasePath = "/users";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenSuccessful()
    {
        // Arrange
        int userId = await CreateUserAsync();
        ChangeUserRoleCommand command = new(["Editor"]);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{userId}/role", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage editResponse = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{userId}/edit", CancellationToken.None);
        UserEditDetails? details = await editResponse.Content.ReadFromJsonAsync<UserEditDetails>(CancellationToken.None);
        details.ShouldNotBeNull();
        details.Roles.ShouldContain("Editor");
    }

    [Fact]
    public async Task ReturnsOk_WhenMultipleRolesAssigned()
    {
        // Arrange
        int userId = await CreateUserAsync();
        ChangeUserRoleCommand command = new(["Admin", "Editor"]);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{userId}/role", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage editResponse = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{userId}/edit", CancellationToken.None);
        UserEditDetails? details = await editResponse.Content.ReadFromJsonAsync<UserEditDetails>(CancellationToken.None);
        details.ShouldNotBeNull();
        details.Roles.ShouldContain("Admin");
        details.Roles.ShouldContain("Editor");
        details.Roles.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        ChangeUserRoleCommand command = new(["Admin"]);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{int.MaxValue}/role", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task ReturnsConflict_WhenChangingOwnRole()
    {
        // Arrange
        int selfUserId = await GetSeededUserIdAsync();
        ChangeUserRoleCommand command = new(["Editor"]);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{selfUserId}/role", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.CannotChangeOwnRole");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenRoleDoesNotExist()
    {
        // Arrange
        int userId = await CreateUserAsync();
        ChangeUserRoleCommand command = new(["SuperAdmin"]);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{userId}/role", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.RoleNotFound");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenRolesEmpty()
    {
        // Arrange
        int userId = await CreateUserAsync();
        ChangeUserRoleCommand command = new([]);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{userId}/role", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.RolesRequired");
    }

    [Fact]
    public async Task ReturnsOk_WhenRoleIsRemovedDuringReconciliation()
    {
        // Arrange
        int userId = await CreateUserAsync();
        ChangeUserRoleCommand assignCommand = new(["Admin", "Editor"]);
        HttpResponseMessage assignResponse = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{userId}/role", assignCommand, CancellationToken.None);
        assignResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        ChangeUserRoleCommand reconcileCommand = new(["Editor"]);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{userId}/role", reconcileCommand, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage editResponse = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{userId}/edit", CancellationToken.None);
        UserEditDetails? details = await editResponse.Content.ReadFromJsonAsync<UserEditDetails>(CancellationToken.None);
        details.ShouldNotBeNull();
        details.Roles.ShouldContain("Editor");
        details.Roles.ShouldNotContain("Admin");
        details.Roles.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        int userId = await CreateUserAsync();
        ChangeUserRoleCommand command = new(["Editor"]);

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PatchAsJsonAsync(
            $"{BasePath}/{userId}/role", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        ChangeUserRoleCommand command = new(["Editor"]);

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PatchAsJsonAsync(
            $"{BasePath}/1/role", command, CancellationToken.None);

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