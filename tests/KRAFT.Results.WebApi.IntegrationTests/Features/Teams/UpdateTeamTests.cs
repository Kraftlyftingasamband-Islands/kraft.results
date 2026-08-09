using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

[Collection(nameof(TeamsCollection))]
public sealed class UpdateTeamTests(CollectionFixture fixture)
{
    private const string BasePath = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

    [Fact]
    public async Task ReturnsOk_WhenSuccessful()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateClubCommand command = new UpdateClubCommandBuilder()
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
        UpdateClubCommand command = new UpdateClubCommandBuilder().Build();

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
        UpdateClubCommand command = new UpdateClubCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        UpdateClubCommand command = new UpdateClubCommandBuilder().Build();

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
        UpdateClubCommand command = new UpdateClubCommandBuilder()
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
        UpdateClubCommand command = new UpdateClubCommandBuilder()
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
        UpdateClubCommand command = new UpdateClubCommandBuilder()
            .WithTitleShort(value)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenCountryCodeIsInvalid()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        UpdateClubCommand command = new UpdateClubCommandBuilder()
            .WithCountryCode("XYZ")
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
        CreateClubCommand otherTeam = new CreateClubCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        HttpResponseMessage otherTeamResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, otherTeam, CancellationToken.None);
        otherTeamResponse.EnsureSuccessStatusCode();

        UpdateClubCommand command = new UpdateClubCommandBuilder()
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

    [Fact]
    public async Task ReturnsConflict_WithErrorCode_WhenTitleAlreadyExists()
    {
        // Arrange
        string slug = await CreateTeamAsync();
        string existingTitle = "Title-" + Guid.NewGuid().ToString()[..8];
        CreateClubCommand otherTeam = new CreateClubCommandBuilder()
            .WithTitle(existingTitle)
            .Build();
        HttpResponseMessage otherTeamResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, otherTeam, CancellationToken.None);
        otherTeamResponse.EnsureSuccessStatusCode();

        UpdateClubCommand command = new UpdateClubCommandBuilder()
            .WithTitle(existingTitle)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe(ErrorCodes.TeamsTitleExists);
    }

    [Fact]
    public async Task ReturnsOk_WhenUpdatingTeamWithItsOwnTitle()
    {
        // Arrange
        string ownTitle = "Title-" + Guid.NewGuid().ToString()[..8];
        string slug = await CreateTeamAsync(ownTitle);
        UpdateClubCommand command = new UpdateClubCommandBuilder()
            .WithTitle(ownTitle)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug}", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsConflict_NotInternalServerError_WhenConcurrentUpdateRacesOnTitle()
    {
        // Arrange — two teams that will both try to adopt a brand-new title simultaneously
        string slug1 = await CreateTeamAsync();
        string slug2 = await CreateTeamAsync();
        string newTitle = "Title-" + Guid.NewGuid().ToString()[..8];

        UpdateClubCommand command1 = new UpdateClubCommandBuilder()
            .WithTitle(newTitle)
            .Build();
        UpdateClubCommand command2 = new UpdateClubCommandBuilder()
            .WithTitle(newTitle)
            .Build();

        // Act — fire both updates simultaneously so both pass AnyAsync and the DB unique constraint fires on one
        Task<HttpResponseMessage> task1 = _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug1}", command1, CancellationToken.None);
        Task<HttpResponseMessage> task2 = _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug2}", command2, CancellationToken.None);
        HttpResponseMessage[] responses = await Task.WhenAll(task1, task2);

        // Assert — neither request may return 500; at least one must return 409
        responses.ShouldAllBe(r => r.StatusCode != HttpStatusCode.InternalServerError);
        responses.ShouldContain(r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsConflict_NotInternalServerError_WhenConcurrentUpdateRacesOnShortTitle()
    {
        // Arrange — two teams that will both try to adopt the same short title simultaneously
        string slug1 = await CreateTeamAsync();
        string slug2 = await CreateTeamAsync();
        string newShortTitle = UniqueShortCode.Next();

        UpdateClubCommand command1 = new UpdateClubCommandBuilder()
            .WithTitleShort(newShortTitle)
            .Build();
        UpdateClubCommand command2 = new UpdateClubCommandBuilder()
            .WithTitleShort(newShortTitle)
            .Build();

        // Act — fire both updates simultaneously so both pass AnyAsync and the DB unique constraint fires on one
        Task<HttpResponseMessage> task1 = _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug1}", command1, CancellationToken.None);
        Task<HttpResponseMessage> task2 = _authorizedHttpClient.PutAsJsonAsync($"{BasePath}/{slug2}", command2, CancellationToken.None);
        HttpResponseMessage[] responses = await Task.WhenAll(task1, task2);

        // Assert — neither request may return 500; at least one must return 409
        responses.ShouldAllBe(r => r.StatusCode != HttpStatusCode.InternalServerError);
        responses.ShouldContain(r => r.StatusCode == HttpStatusCode.Conflict);
    }

    private async Task<string> CreateTeamAsync(string? title = null)
    {
        CreateClubCommand createCommand = title is null
            ? new CreateClubCommandBuilder().Build()
            : new CreateClubCommandBuilder().WithTitle(title).Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        List<ClubSummary>? teamsResponse = await _authorizedHttpClient.GetFromJsonAsync<List<ClubSummary>>(BasePath, CancellationToken.None);
        List<ClubSummary> teams = teamsResponse.ShouldNotBeNull();
        ClubSummary team = teams.First(t => t.ShortTitle == createCommand.TitleShort);

        return team.Slug;
    }
}
