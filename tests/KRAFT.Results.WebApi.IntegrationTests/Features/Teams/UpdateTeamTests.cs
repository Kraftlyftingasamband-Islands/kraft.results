using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

public sealed class UpdateTeamTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenSuccessful()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateTeamCommand command = new UpdateTeamCommandBuilder()
            .WithTitle("Updated Title")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsNotFound_WithErrorCode_WhenTeamDoesNotExist()
    {
        // Arrange
        UpdateTeamCommand command = new UpdateTeamCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/non-existent-slug", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Teams.NotFound");
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateTeamCommand command = new UpdateTeamCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        UpdateTeamCommand command = new UpdateTeamCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsJsonAsync($"{BasePath}/some-slug", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateTeamCommand command = new UpdateTeamCommandBuilder()
            .WithTitle(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenFullTitleIsEmpty()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateTeamCommand command = new UpdateTeamCommandBuilder()
            .WithTitleFull(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ABCD")]
    public async Task ReturnsBadRequest_WhenShortTitleIsInvalid(string value)
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateTeamCommand command = new UpdateTeamCommandBuilder()
            .WithTitleShort(value)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenCountryDoesNotExist()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateTeamCommand command = new UpdateTeamCommandBuilder()
            .WithCountryId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsConflict_WithErrorCode_WhenShortTitleAlreadyExists()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        string shortTitle = "ZZZ";
        CreateTeamCommand otherTeam = new CreateTeamCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        await _authorizedHttpClient.PostAsJsonAsync(BasePath, otherTeam, CancellationToken.None);

        UpdateTeamCommand command = new UpdateTeamCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe(ErrorCodes.TeamsShortTitleExists);
    }

    private async Task<string> CreateTeamAsync()
    {
        CreateTeamCommand createCommand = new CreateTeamCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        List<TeamSummary>? teams = await _authorizedHttpClient.GetFromJsonAsync<List<TeamSummary>>(BasePath, CancellationToken.None);
        TeamSummary team = teams!.First(t => t.ShortTitle == createCommand.TitleShort);

        return team.Slug;
    }
}