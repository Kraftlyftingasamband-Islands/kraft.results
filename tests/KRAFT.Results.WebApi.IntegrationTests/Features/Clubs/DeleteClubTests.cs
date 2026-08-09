using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Clubs;

[Collection(nameof(ClubsCollection))]
public sealed class DeleteClubTests(CollectionFixture fixture)
{
    private const string BasePath = "/teams";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        string slug = await CreateClubAsync();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{slug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WithErrorCode_WhenTeamDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/non-existent-slug", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Teams.NotFound");
    }

    [Fact]
    public async Task ReturnsConflict_WithErrorCode_WhenTeamHasAthletes()
    {
        // Arrange
        (string slug, int clubId) = await CreateClubWithIdAsync();

        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithClubId(clubId)
            .Build();
        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync("/athletes", athleteCommand, CancellationToken.None);
        athleteResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync($"{BasePath}/{slug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Teams.HasAthletes");
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        string slug = await CreateClubAsync();

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.DeleteAsync($"{BasePath}/{slug}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.DeleteAsync($"{BasePath}/some-slug", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> CreateClubAsync()
    {
        (string slug, _) = await CreateClubWithIdAsync();
        return slug;
    }

    private async Task<(string Slug, int ClubId)> CreateClubWithIdAsync()
    {
        CreateClubCommand createCommand = new CreateClubCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(BasePath, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        string? location = createResponse.Headers.Location?.ToString();
        location.ShouldNotBeNull();
        int clubId = int.Parse(location.TrimStart('/'), System.Globalization.CultureInfo.InvariantCulture);

        List<ClubSummary>? clubs = await _authorizedHttpClient.GetFromJsonAsync<List<ClubSummary>>(BasePath, CancellationToken.None);
        ClubSummary club = clubs!.First(t => t.ShortTitle == createCommand.TitleShort);

        return (club.Slug, clubId);
    }
}
