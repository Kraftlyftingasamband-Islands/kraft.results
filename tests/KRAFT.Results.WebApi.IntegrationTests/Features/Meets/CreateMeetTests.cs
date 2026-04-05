using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class CreateMeetTests(IntegrationTestFixture fixture)
{
    private const string Path = "/meets";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenHttpClientIsUnauthorized()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsConflict_WithErrorCode_WhenMeetExists()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder().Build();
        _ = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.AlreadyExists");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithTitle(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDateIsBefore1900()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(1899, 1, 1))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenMeetTypeDoesNotExist()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithMeetTypeId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2025, 6, 15))
            .WithEndDate(new DateOnly(2025, 6, 14))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.EndDateBeforeStartDate");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenResultModeIsInvalid()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithResultModeId(99)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.InvalidResultMode");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenResultModeIsZero()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithResultModeId(0)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.InvalidResultMode");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTextIsTooLong()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithText(new string('A', 4001))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.TextTooLong");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenLocationIsTooLong()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithLocation(new string('A', 51))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.LocationTooLong");
    }

    [Fact]
    public async Task ReturnsCreated_WhenEndDateIsNull()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithEndDate(null)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsCreated_WhenEndDateEqualsStartDate()
    {
        // Arrange
        DateOnly date = new(2025, 6, 15);
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(date)
            .WithEndDate(date)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsCreated_WhenAllSettingsFieldsAreProvided()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2025, 6, 15))
            .WithEndDate(new DateOnly(2025, 6, 16))
            .WithCalcPlaces(false)
            .WithText("Some description")
            .WithLocation("Reykjavik")
            .WithPublishedResults(false)
            .WithResultModeId(2)
            .WithPublishedInCalendar(false)
            .WithIsInTeamCompetition(true)
            .WithShowWilks(false)
            .WithShowTeamPoints(false)
            .WithShowBodyWeight(false)
            .WithShowTeams(true)
            .WithRecordsPossible(false)
            .WithIsRaw(true)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}