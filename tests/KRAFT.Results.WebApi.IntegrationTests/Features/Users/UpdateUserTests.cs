using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

[Collection(nameof(UsersCollection))]
public sealed class UpdateUserTests(CollectionFixture fixture)
{
    private const string BasePath = "/users";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenSuccessful()
    {
        // Arrange
        int userId = await CreateUserAsync();
        UpdateUserCommand command = new UpdateUserCommandBuilder()
            .WithFirstName("Updated First")
            .WithLastName("Updated Last")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{userId}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsNotFound_WithErrorCode_WhenUserDoesNotExist()
    {
        // Arrange
        UpdateUserCommand command = new UpdateUserCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{int.MaxValue}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        int userId = await CreateUserAsync();
        UpdateUserCommand command = new UpdateUserCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PutAsJsonAsync(
            $"{BasePath}/{userId}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        UpdateUserCommand command = new UpdateUserCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/1", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenFirstNameIsEmpty()
    {
        // Arrange
        int userId = await CreateUserAsync();
        UpdateUserCommand command = new UpdateUserCommandBuilder()
            .WithFirstName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{userId}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenLastNameIsEmpty()
    {
        // Arrange
        int userId = await CreateUserAsync();
        UpdateUserCommand command = new UpdateUserCommandBuilder()
            .WithLastName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{userId}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        int userId = await CreateUserAsync();
        UpdateUserCommand command = new UpdateUserCommandBuilder()
            .WithEmail("not-an-email")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{userId}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsConflict_WhenEmailIsTakenByAnotherUser()
    {
        // Arrange
        int userId = await CreateUserAsync();
        UpdateUserCommand command = new UpdateUserCommandBuilder()
            .WithEmail(Constants.TestUser.Email)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{userId}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.EmailExists");
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
}