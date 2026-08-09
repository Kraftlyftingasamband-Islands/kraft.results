using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

[Collection(nameof(TeamsCollection))]
public sealed class CreateTeamTests(CollectionFixture fixture)
{
    private const string Path = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

    [Fact]
    public async Task ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        CreateClubCommand command = new CreateClubCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        CreateClubCommand command = new CreateClubCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenHttpClientIsUnauthorized()
    {
        // Arrange
        CreateClubCommand command = new CreateClubCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        CreateClubCommand command = new CreateClubCommandBuilder()
            .WithTitle(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenFullTitleIsEmpty()
    {
        // Arrange
        CreateClubCommand command = new CreateClubCommandBuilder()
            .WithTitleFull(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

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
        CreateClubCommand command = new CreateClubCommandBuilder()
            .WithTitleShort(value)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsConflict_WithErrorCode_WhenShortTitleExists()
    {
        // Arrange
        string shortTitle = "ABC";
        CreateClubCommand firstCommand = new CreateClubCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        CreateClubCommand secondCommand = new CreateClubCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        HttpResponseMessage firstResponse = await _authorizedHttpClient.PostAsJsonAsync(Path, firstCommand, CancellationToken.None);
        firstResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, secondCommand, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe(ErrorCodes.TeamsShortTitleExists);
    }

    [Fact]
    public async Task ReturnsConflict_WithErrorCode_WhenTitleExists()
    {
        // Arrange
        string title = "Title-" + Guid.NewGuid().ToString()[..8];
        CreateClubCommand firstCommand = new CreateClubCommandBuilder()
            .WithTitle(title)
            .Build();
        CreateClubCommand secondCommand = new CreateClubCommandBuilder()
            .WithTitle(title)
            .Build();
        HttpResponseMessage firstResponse = await _authorizedHttpClient.PostAsJsonAsync(Path, firstCommand, CancellationToken.None);
        firstResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, secondCommand, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe(ErrorCodes.TeamsTitleExists);
    }

    [Fact]
    public async Task ReturnsConflict_NotInternalServerError_WhenConcurrentCreationRacesOnTitle()
    {
        // Arrange
        string title = "Title-" + Guid.NewGuid().ToString()[..8];
        CreateClubCommand firstCommand = new CreateClubCommandBuilder()
            .WithTitle(title)
            .Build();
        CreateClubCommand secondCommand = new CreateClubCommandBuilder()
            .WithTitle(title)
            .Build();

        // Act — fire both requests simultaneously so the DB unique constraint may fire
        Task<HttpResponseMessage> task1 = _authorizedHttpClient.PostAsJsonAsync(Path, firstCommand, CancellationToken.None);
        Task<HttpResponseMessage> task2 = _authorizedHttpClient.PostAsJsonAsync(Path, secondCommand, CancellationToken.None);
        HttpResponseMessage[] responses = await Task.WhenAll(task1, task2);

        // Assert — neither request may return 500; at least one must return 409
        responses.ShouldAllBe(r => r.StatusCode != HttpStatusCode.InternalServerError);
        responses.ShouldContain(r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsConflict_NotInternalServerError_WhenConcurrentCreationRacesOnShortTitle()
    {
        // Arrange
        string shortTitle = UniqueShortCode.Next();
        CreateClubCommand firstCommand = new CreateClubCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        CreateClubCommand secondCommand = new CreateClubCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();

        // Act — fire both requests simultaneously so the DB unique constraint may fire
        Task<HttpResponseMessage> task1 = _authorizedHttpClient.PostAsJsonAsync(Path, firstCommand, CancellationToken.None);
        Task<HttpResponseMessage> task2 = _authorizedHttpClient.PostAsJsonAsync(Path, secondCommand, CancellationToken.None);
        HttpResponseMessage[] responses = await Task.WhenAll(task1, task2);

        // Assert — neither request may return 500; at least one must return 409
        responses.ShouldAllBe(r => r.StatusCode != HttpStatusCode.InternalServerError);
        responses.ShouldContain(r => r.StatusCode == HttpStatusCode.Conflict);
    }
}
