using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class LoginTests(IntegrationTestFixture fixture)
{
    private const string Path = "/users/login";

    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenSuccessful()
    {
        // Arrange
        LoginCommand body = new LoginCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange
        LoginCommand body = new LoginCommandBuilder().Build();

        // Act
        HttpResponseMessage message = await _unauthorizedHttpClient.PostAsJsonAsync(Path, body, CancellationToken.None);
        AuthenticatedResponse? response = await message.Content.ReadFromJsonAsync<AuthenticatedResponse>(CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenUsernameIsEmpty()
    {
        // Arrange
        LoginCommand body = new LoginCommandBuilder()
            .WithUsername(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        // Arrange
        LoginCommand body = new LoginCommandBuilder()
            .WithUsername(Guid.NewGuid().ToString())
            .Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenPasswordIsEmpty()
    {
        // Arrange
        LoginCommand body = new LoginCommandBuilder()
            .WithPassword(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenPasswordIsWrong()
    {
        // Arrange
        LoginCommand body = new LoginCommandBuilder()
            .WithPassword(Guid.NewGuid().ToString())
            .Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}