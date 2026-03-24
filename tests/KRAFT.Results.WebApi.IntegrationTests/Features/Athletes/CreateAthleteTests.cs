using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

public class CreateAthleteTests(IntegrationTestFixture fixture)
{
    private const string Path = "/athletes";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenHttpClientIsUnauthorized()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsConflict_WhenAthleteExists()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(Constants.TestAthleteFirstName)
            .WithLastName(Constants.TestAthleteLastName)
            .WithDateOfBirth(Constants.TestAthleteDateOfBirth)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenCountryIdDoesNotExist()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithCountryId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTeamIdDoesNotExist()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithTeamId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenFirstNameIsEmpty()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenLastNameIsEmpty()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithLastName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenGenderIsInvalid()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithGender("X")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDateOfBirthIsInTheFuture()
    {
        // Arrange
        DateOnly futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(futureDate)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}