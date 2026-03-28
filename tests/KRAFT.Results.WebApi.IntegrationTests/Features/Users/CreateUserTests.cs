using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class CreateUserTests
{
    private const string Path = "/users";

    private readonly HttpClient _authorizedHttpClient;
    private readonly HttpClient _unauthorizedHttpClient;

    public CreateUserTests(IntegrationTestFixture fixture)
    {
        _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
        _unauthorizedHttpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        CreateUserCommand body = new CreateUserCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenHttpClientIsUnauthorized()
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsConflict_WithDescription_WhenUsernameExists()
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder()
            .WithUsername(Constants.TestUser.Username)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        body.ShouldContain("User name already exists");
    }

    [Fact]
    public async Task ReturnsConflict_WithDescription_WhenEmailExists()
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder()
            .WithEmail(Constants.TestUser.Email)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        body.ShouldContain("already a user with that e-mail");
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenUsernameIsInvalid(string username)
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder()
            .WithUsername(username)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenFirstNameIsInvalid(string username)
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder()
            .WithFirstName(username)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenLastNameIsInvalid(string username)
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder()
            .WithLastName(username)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("@")]
    [InlineData("a@")]
    [InlineData("@a")]
    public async Task ReturnsBadRequest_WhenEmailIsInvalid(string username)
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder()
            .WithEmail(username)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenPasswordIsInvalid(string username)
    {
        // Arrange
        CreateUserCommand command = new CreateUserCommandBuilder()
            .WithPassword(username)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}