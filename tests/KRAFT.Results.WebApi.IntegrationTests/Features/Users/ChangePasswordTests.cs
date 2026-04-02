using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class ChangePasswordTests(IntegrationTestFixture fixture)
{
    private const string ChangePasswordPath = "/users/change-password";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenPasswordChangedSuccessfully()
    {
        // Arrange
        string newPassword = "NewSecurePassword123";
        ChangePasswordCommand command = new ChangePasswordCommandBuilder()
            .WithNewPassword(newPassword)
            .WithConfirmNewPassword(newPassword)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            ChangePasswordPath, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Restore original password so other tests are not affected
        ChangePasswordCommand restoreCommand = new(newPassword, Constants.TestUser.Password, Constants.TestUser.Password);
        HttpResponseMessage restoreResponse = await _authorizedHttpClient.PostAsJsonAsync(
            ChangePasswordPath, restoreCommand, CancellationToken.None);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenCurrentPasswordIsIncorrect()
    {
        // Arrange
        ChangePasswordCommand command = new ChangePasswordCommandBuilder()
            .WithCurrentPassword("wrong-password")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            ChangePasswordPath, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.IncorrectCurrentPassword");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenPasswordsDoNotMatch()
    {
        // Arrange
        ChangePasswordCommand command = new ChangePasswordCommandBuilder()
            .WithCurrentPassword("any-password")
            .WithNewPassword("NewPassword123")
            .WithConfirmNewPassword("DifferentPassword123")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            ChangePasswordPath, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Users.PasswordsDoNotMatch");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenNewPasswordIsEmpty()
    {
        // Arrange
        ChangePasswordCommand command = new ChangePasswordCommandBuilder()
            .WithCurrentPassword("any-password")
            .WithNewPassword(string.Empty)
            .WithConfirmNewPassword(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            ChangePasswordPath, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Password.Empty");
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        ChangePasswordCommand command = new ChangePasswordCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(
            ChangePasswordPath, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}