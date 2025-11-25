using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

public class CreateAthleteTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _httpClient;

    public CreateAthleteTests(IntegrationTestFixture fixture)
    {
        _httpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsCreated_WhenBodyIsValid()
    {
        // Arrange
        var body = new AthleteBuilder().Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/athletes", body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenCountryIdDoesNotExist()
    {
        // Arrange
        var body = new AthleteBuilder()
            .WithCountryId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/athletes", body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTeamIdDoesNotExist()
    {
        // Arrange
        var body = new AthleteBuilder()
            .WithTeamId(int.MaxValue)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/athletes", body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenFirstNameIsEmpty()
    {
        // Arrange
        var body = new AthleteBuilder()
            .WithFirstName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/athletes", body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenLastNameIsEmpty()
    {
        // Arrange
        var body = new AthleteBuilder()
            .WithLastName(string.Empty)
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/athletes", body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenGenderIsInvalid()
    {
        // Arrange
        var body = new AthleteBuilder()
            .WithGender("X")
            .Build();

        // Act
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/athletes", body, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}