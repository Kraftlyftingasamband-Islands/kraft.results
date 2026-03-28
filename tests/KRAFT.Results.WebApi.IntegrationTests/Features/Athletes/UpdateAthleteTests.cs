using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

public sealed class UpdateAthleteTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/athletes";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenSuccessful()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder()
            .WithFirstName("Updated First")
            .WithLastName("Updated Last")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsNotFound_WithErrorCode_WhenAthleteDoesNotExist()
    {
        // Arrange
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/non-existent-slug", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Athletes.NotFound");
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/some-slug", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenFirstNameIsEmpty()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder()
            .WithFirstName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenLastNameIsEmpty()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder()
            .WithLastName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenGenderIsInvalid()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder()
            .WithGender("X")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenCountryDoesNotExist()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder()
            .WithCountryId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTeamDoesNotExist()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder()
            .WithTeamId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDateOfBirthIsInTheFuture()
    {
        // Arrange
        string slug = await CreateAthleteAsync();
        DateOnly futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        UpdateAthleteCommand command = new UpdateAthleteCommandBuilder()
            .WithDateOfBirth(futureDate)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<string> CreateAthleteAsync()
    {
        CreateAthleteCommand createCommand = new CreateAthleteCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        List<AthleteSummary>? athletes = await _authorizedHttpClient.GetFromJsonAsync<List<AthleteSummary>>(BasePath, CancellationToken.None);
        AthleteSummary athlete = athletes!.First(a => a.Name == $"{createCommand.FirstName} {createCommand.LastName}");

        return athlete.Slug!;
    }
}