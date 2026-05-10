using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(CalcPlacesToggleTestsCollection))]
public sealed class CalcPlacesToggleTests(CollectionFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<(int MeetId, string MeetSlug)> _meets = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        foreach ((int meetId, int participationId) in Enumerable.Reverse(_participations))
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{meetId}/participants/{participationId}", CancellationToken.None);
        }

        foreach ((int _, string meetSlug) in Enumerable.Reverse(_meets))
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{meetSlug}", CancellationToken.None);
        }

        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task RecomputesPlacesPerWeightCategoryGroup_WhenToggledFromFalseToTrue()
    {
        // Arrange — create a meet with CalcPlaces=false so attempts do not trigger place computation
        (int meetId, string meetSlug) = await CreateMeetAsync(calcPlaces: false);

        // Athlete 1 in 83kg group (body weight 80.5 kg) — higher total within own group
        string athlete1Slug = await CreateAthleteAsync();
        int participation1Id = await AddParticipantAsync(meetId, athlete1Slug, bodyWeight: 80.5m);
        await RecordFullTotalAsync(meetId, participation1Id, squat: 200.0m, bench: 130.0m, deadlift: 250.0m);

        // Athlete 2 in 93kg group (body weight 88.0 kg) — only lifter in this group
        string athlete2Slug = await CreateAthleteAsync();
        int participation2Id = await AddParticipantAsync(meetId, athlete2Slug, bodyWeight: 88.0m);
        await RecordFullTotalAsync(meetId, participation2Id, squat: 180.0m, bench: 110.0m, deadlift: 210.0m);

        // Act — toggle CalcPlaces to true; this triggers RecomputeMeetAsync(calcPlaces: true)
        UpdateMeetCommand updateCommand = new UpdateMeetCommandBuilder()
            .WithCalcPlaces(true)
            .Build();

        HttpResponseMessage updateResponse = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetSlug}",
            updateCommand,
            CancellationToken.None);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert — each athlete is ranked 1st within their own weight category group
        List<MeetParticipation>? participations = await _authorizedHttpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{meetSlug}/participations",
            CancellationToken.None);

        participations.ShouldNotBeNull();
        MeetParticipation? p1 = participations.FirstOrDefault(p => p.ParticipationId == participation1Id);
        MeetParticipation? p2 = participations.FirstOrDefault(p => p.ParticipationId == participation2Id);

        p1.ShouldNotBeNull();
        p2.ShouldNotBeNull();

        // Both are ranked 1st because they are in separate weight category groups
        p1.Rank.ShouldBe(1);
        p2.Rank.ShouldBe(1);
    }

    private async Task<(int MeetId, string MeetSlug)> CreateMeetAsync(bool calcPlaces)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithCalcPlaces(calcPlaces)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? details = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}",
            CancellationToken.None);

        (int meetId, string meetSlug) = (details!.MeetId, slug);
        _meets.Add((meetId, meetSlug));
        return (meetId, meetSlug);
    }

    private async Task<string> CreateAthleteAsync()
    {
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithCountryCode("NOR")
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{command.FirstName} {command.LastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug, decimal bodyWeight)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .WithAgeCategorySlug("open")
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content.ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);
        int participationId = result!.ParticipationId;
        _participations.Add((meetId, participationId));
        return participationId;
    }

    private async Task RecordFullTotalAsync(int meetId, int participationId, decimal squat, decimal bench, decimal deadlift)
    {
        await RecordAttemptAsync(meetId, participationId, Discipline.Squat, 1, squat, good: true);
        await RecordAttemptAsync(meetId, participationId, Discipline.Bench, 1, bench, good: true);
        await RecordAttemptAsync(meetId, participationId, Discipline.Deadlift, 1, deadlift, good: true);
    }

    private async Task RecordAttemptAsync(int meetId, int participationId, Discipline discipline, int round, decimal weight, bool good)
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