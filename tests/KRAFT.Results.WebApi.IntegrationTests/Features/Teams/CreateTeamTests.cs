using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Teams;

public sealed class CreateTeamTests : IClassFixture<IntegrationTestFixture>
{
    private const string Root = "/teams";

    private readonly HttpClient _httpClient;

    public CreateTeamTests(IntegrationTestFixture fixture)
    {
        _httpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsCreated_WhenBodyIsValid()
    {
        // Arrange
        var body = new CreateTeamCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Root, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var body = new CreateTeamCommandBuilder()
            .WithTitle(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Root, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenFullTitleIsEmpty()
    {
        // Arrange
        var body = new CreateTeamCommandBuilder()
            .WithTitleFull(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Root, body, CancellationToken.None);

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
        var body = new CreateTeamCommandBuilder()
            .WithTitleShort(value)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Root, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenShortTitleExists()
    {
        // Arrange
        string shortTitle = "ABC";
        var firstBody = new CreateTeamCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        var secondBody = new CreateTeamCommandBuilder()
            .WithTitleShort(shortTitle)
            .Build();
        _ = await _httpClient.PostAsJsonAsync(Root, firstBody, CancellationToken.None);

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Root, secondBody, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}