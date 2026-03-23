using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

public sealed class CreateTeamTests(IntegrationTestFixture fixture)
{
    private const string Path = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        CreateTeamCommand command = new CreateTeamCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        CreateTeamCommand command = new CreateTeamCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenHttpClientIsUnauthorized()
    {
        // Arrange
        CreateTeamCommand command = new CreateTeamCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        CreateTeamCommand command = new CreateTeamCommandBuilder()
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
        CreateTeamCommand command = new CreateTeamCommandBuilder()
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
        CreateTeamCommand command = new CreateTeamCommandBuilder()
            .WithTitleShort(value)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenShortTitleExists()
    {
        // Arrange
        string shortTitle = "ABC";
        CreateTeamCommand firstCommand = new CreateTeamCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        CreateTeamCommand secondCommand = new CreateTeamCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        _ = await _authorizedHttpClient.PostAsJsonAsync(Path, firstCommand, CancellationToken.None);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(Path, secondCommand, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}