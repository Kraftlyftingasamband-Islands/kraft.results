using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class RemoveParticipantTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const int NonExistentMeetId = 99999;
    private const int NonExistentParticipationId = 99999;
    private const int IrrelevantParticipationId = int.MaxValue;

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private int _meetId;
    private string _meetSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder().Build();

        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync("/meets", command, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        _meetSlug = createResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meetSlug}", CancellationToken.None);
        _meetId = details!.MeetId;
    }

    public async ValueTask DisposeAsync()
    {
        if (_meetId != 0)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{_meetSlug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _nonAdminHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        int participationId = await AddParticipantAsync(_meetId);

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{participationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenParticipationDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{NonExistentParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.ParticipationNotFound");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{NonExistentMeetId}/participants/{NonExistentParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{IrrelevantParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.DeleteAsync(
            $"/meets/{_meetId}/participants/{IrrelevantParticipationId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReranksSurvivingParticipants_WhenSecondPlaceParticipantRemoved()
    {
        // Arrange
        (int meetId, string meetSlug) = await CreateMeetAsync(new CreateMeetCommandBuilder());

        (int firstPlaceId, string firstAthleteSlug) = await AddParticipantToMeetAsync(meetId);
        (int secondPlaceId, string secondAthleteSlug) = await AddParticipantToMeetAsync(meetId);
        (int thirdPlaceId, string thirdAthleteSlug) = await AddParticipantToMeetAsync(meetId);

        // First place — highest total (300 kg)
        await RecordAttemptForMeet(meetId, firstPlaceId, Discipline.Squat, 1, 100.0m, true);
        await RecordAttemptForMeet(meetId, firstPlaceId, Discipline.Bench, 1, 50.0m, true);
        await RecordAttemptForMeet(meetId, firstPlaceId, Discipline.Deadlift, 1, 150.0m, true);

        // Second place — middle total (240 kg)
        await RecordAttemptForMeet(meetId, secondPlaceId, Discipline.Squat, 1, 80.0m, true);
        await RecordAttemptForMeet(meetId, secondPlaceId, Discipline.Bench, 1, 40.0m, true);
        await RecordAttemptForMeet(meetId, secondPlaceId, Discipline.Deadlift, 1, 120.0m, true);

        // Third place — lowest total (180 kg)
        await RecordAttemptForMeet(meetId, thirdPlaceId, Discipline.Squat, 1, 60.0m, true);
        await RecordAttemptForMeet(meetId, thirdPlaceId, Discipline.Bench, 1, 30.0m, true);
        await RecordAttemptForMeet(meetId, thirdPlaceId, Discipline.Deadlift, 1, 90.0m, true);

        // Act — remove the 2nd-place participant
        HttpResponseMessage deleteResponse = await _authorizedHttpClient.DeleteAsync(
            $"/meets/{meetId}/participants/{secondPlaceId}",
            CancellationToken.None);
        deleteResponse.EnsureSuccessStatusCode();

        // Assert — former 3rd-place participant should now be ranked 2nd
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{meetSlug}/participations",
                CancellationToken.None);

        participations.ShouldNotBeNull();
        MeetParticipation? first = participations.FirstOrDefault(p => p.ParticipationId == firstPlaceId);
        MeetParticipation? third = participations.FirstOrDefault(p => p.ParticipationId == thirdPlaceId);
        MeetParticipation? removed = participations.FirstOrDefault(p => p.ParticipationId == secondPlaceId);

        first.ShouldNotBeNull();
        third.ShouldNotBeNull();
        removed.ShouldBeNull();
        first.Rank.ShouldBe(1);
        third.Rank.ShouldBe(2);

        // Cleanup — secondPlaceId already removed during Act; delete remaining participants first
        (await _authorizedHttpClient.DeleteAsync($"/meets/{meetId}/participants/{firstPlaceId}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/meets/{meetId}/participants/{thirdPlaceId}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/meets/{meetSlug}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/athletes/{firstAthleteSlug}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/athletes/{secondAthleteSlug}", CancellationToken.None)).EnsureSuccessStatusCode();
        (await _authorizedHttpClient.DeleteAsync($"/athletes/{thirdAthleteSlug}", CancellationToken.None)).EnsureSuccessStatusCode();
    }

    private async Task<int> AddParticipantAsync(int meetId)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.TestAthleteSlug)
            .WithBodyWeight(82.5m)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        AddParticipantResponse? body = await response.Content.ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);
        body.ShouldNotBeNull();

        return body.ParticipationId;
    }

    private async Task<(int MeetId, string MeetSlug)> CreateMeetAsync(CreateMeetCommandBuilder builder)
    {
        CreateMeetCommand command = builder.Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}",
            CancellationToken.None);

        return (details!.MeetId, slug);
    }

    private async Task<(int ParticipationId, string AthleteSlug)> AddParticipantToMeetAsync(int meetId)
    {
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().WithCountryId(2).Build();
        HttpResponseMessage athleteResponse = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .Build();

        HttpResponseMessage participantResponse = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants",
            participantCommand,
            CancellationToken.None);

        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return (result!.ParticipationId, athleteSlug);
    }

    private async Task RecordAttemptForMeet(
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight,
        bool good)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(good)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}