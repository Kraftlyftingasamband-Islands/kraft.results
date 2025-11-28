using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class CreateUserTests
{
    private const string Path = "/users";

    private readonly HttpClient _httpClient;

    public CreateUserTests(IntegrationTestFixture fixture)
    {
        _httpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsCreated_WhenBodyIsValid()
    {
        // Arrange
        var body = new CreateUserCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsConflict_WhenUsernameExists()
    {
        // Arrange
        string username = "SomeUser";
        var firstBody = new CreateUserCommandBuilder()
            .WithUsername(username)
            .Build();
        var secondBody = new CreateUserCommandBuilder()
            .WithUsername(username)
            .Build();
        await _httpClient.PostAsJsonAsync(Path, firstBody, CancellationToken.None);

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, secondBody, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsConflict_WhenEmailExists()
    {
        // Arrange
        string email = "somebody@something.com";
        var firstBody = new CreateUserCommandBuilder()
            .WithEmail(email)
            .Build();
        var secondBody = new CreateUserCommandBuilder()
            .WithEmail(email)
            .Build();
        await _httpClient.PostAsJsonAsync(Path, firstBody, CancellationToken.None);

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, secondBody, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenUsernameIsInvalid(string username)
    {
        // Arrange
        var body = new CreateUserCommandBuilder()
            .WithUsername(username)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenFirstNameIsInvalid(string username)
    {
        // Arrange
        var body = new CreateUserCommandBuilder()
            .WithFirstName(username)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenLastNameIsInvalid(string username)
    {
        // Arrange
        var body = new CreateUserCommandBuilder()
            .WithLastName(username)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

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
        var body = new CreateUserCommandBuilder()
            .WithEmail(username)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    public async Task ReturnsBadRequest_WhenPasswordIsInvalid(string username)
    {
        // Arrange
        var body = new CreateUserCommandBuilder()
            .WithPassword(username)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}