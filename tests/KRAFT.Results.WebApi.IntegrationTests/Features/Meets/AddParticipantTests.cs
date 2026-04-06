using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class AddParticipantTests(IntegrationTestFixture fixture)
{
    private const int ExistingMeetId = 2;
    private const int MeetWithExistingParticipationId = 1;
    private const int NonExistentMeetId = 99999;
    private const int MeetForTeamTest = 5;
    private const int NegativeBodyWeightTestMeetId = 4;
    private const int ExistingTeamId = 1;

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(82.5m)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{ExistingMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsCreatedWithResponse_WhenSuccessful()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(80.5m)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/3/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        AddParticipantResponse? body = await response.Content.ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);
        body.ShouldNotBeNull();
        body.ParticipationId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder().Build();

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PostAsJsonAsync(
            $"/meets/{ExistingMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{NonExistentMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenAthleteSlugDoesNotExist()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug("nonexistent-athlete")
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{ExistingMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsConflict_WhenAthleteAlreadyRegistered()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{MeetWithExistingParticipationId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenBodyWeightExceedsMaximum()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(999m)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{ExistingMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenBodyWeightIsJustAboveMaximum()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(400.001m)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{ExistingMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNameClaimIsMissing()
    {
        // Arrange
        HttpClient noNameClaimHttpClient = fixture.CreateNoNameClaimHttpClient();
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .Build();

        // Act
        HttpResponseMessage response = await noNameClaimHttpClient.PostAsJsonAsync(
            $"/meets/{ExistingMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsCreated_WhenTeamIdProvided()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(82.5m)
            .WithTeamId(ExistingTeamId)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{MeetForTeamTest}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenAgeCategorySlugIsTooLong()
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(82.5m)
            .WithAgeCategorySlug(new string('a', 51))
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{ExistingMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task ReturnsBadRequest_WhenBodyWeightIsZeroOrNegative(decimal bodyWeight)
    {
        // Arrange
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(bodyWeight)
            .Build();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{NegativeBodyWeightTestMeetId}/participants", command, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}