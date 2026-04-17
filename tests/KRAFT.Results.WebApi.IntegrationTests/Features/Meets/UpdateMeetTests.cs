using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class UpdateMeetTests(CollectionFixture fixture)
{
    private const string BasePath = "/meets";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenSuccessful()
    {
        // Arrange
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithTitle("Updated Title")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsNotFound_WithErrorCode_WhenMeetDoesNotExist()
    {
        // Arrange
        UpdateMeetCommand command = new UpdateMeetCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/non-existent-slug", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.NotFound");
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        UpdateMeetCommand command = new UpdateMeetCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsJsonAsync($"{BasePath}/some-slug", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithTitle(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDateIsBefore1900()
    {
        // Arrange
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithStartDate(new DateOnly(1899, 1, 1))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenMeetTypeDoesNotExist()
    {
        // Arrange
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithMeetTypeId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2025, 6, 15))
            .WithEndDate(new DateOnly(2025, 6, 14))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

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
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithResultModeId(99)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

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
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithResultModeId(0)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

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
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithText(new string('A', 8001))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

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
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithLocation(new string('A', 51))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.LocationTooLong");
    }

    [Fact]
    public async Task ReturnsOk_WhenAllSettingsFieldsAreProvided()
    {
        // Arrange
        string slug = await CreateMeetAsync();
        UpdateMeetCommand command = new UpdateMeetCommandBuilder()
            .WithTitle("Updated With Settings")
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
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<string> CreateMeetAsync()
    {
        CreateMeetCommand createCommand = new CreateMeetCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        string? location = createResponse.Headers.Location?.ToString();
        location.ShouldNotBeNull();

        return location.TrimStart('/');
    }
}