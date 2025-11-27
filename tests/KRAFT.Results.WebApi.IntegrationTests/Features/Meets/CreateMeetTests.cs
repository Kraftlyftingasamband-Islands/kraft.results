using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class CreateMeetTests : IClassFixture<IntegrationTestFixture>
{
    private const string Path = "/meets";

    private readonly HttpClient _httpClient;

    public CreateMeetTests(IntegrationTestFixture fixture)
    {
        _httpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsCreated_WhenBodyIsValid()
    {
        // Arrange
        var body = new CreateMeetCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsConflict_WhenMeetExists()
    {
        // Arrange
        var body = new CreateMeetCommandBuilder().Build();
        _ = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var body = new CreateMeetCommandBuilder()
            .WithTitle(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDateIsBefore1900()
    {
        // Arrange
        var body = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(1899, 1, 1))
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenMeetTypeDoesNotExist()
    {
        // Arrange
        var body = new CreateMeetCommandBuilder()
            .WithMeetTypeId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Path, body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}