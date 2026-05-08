using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(BanDisqualificationTestsCollection))]
[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "SQL is composed from compile-time constants and GUID-derived slugs containing only URL-safe characters")]
[SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection", Justification = "SQL is composed from compile-time constants and GUID-derived slugs containing only URL-safe characters")]
public sealed class BanDisqualificationTests(CollectionFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private readonly List<string> _banAthleteSlugsToClear = [];

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        foreach (string slug in _banAthleteSlugsToClear)
        {
            await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
            ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(
                $"DELETE FROM Bans WHERE AthleteId = (SELECT AthleteId FROM Athletes WHERE Slug = '{slug}')",
                CancellationToken.None);
        }

        foreach ((int meetId, int participationId) in _participations)
        {
            await _authorizedHttpClient.DeleteAsync(
                $"/meets/{meetId}/participants/{participationId}", CancellationToken.None);
        }

        foreach (string slug in _meetSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{slug}", CancellationToken.None);
        }

        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task BannedAthlete_IsDisqualified_WithPreservedTotal()
    {
        // Arrange
        (HttpClient recordClient, RecordComputationChannel channel) =
            fixture.CreateAuthorizedHttpClientWithRecordComputation();

        string athleteSlug = await CreateAthleteAsync("BanDq", "m", new DateOnly(1990, 6, 15));
        _banAthleteSlugsToClear.Add(athleteSlug);

        (int meetId, string meetSlug) = await CreateMeetAndGetIdAsync(new DateOnly(2025, 3, 15));

        int participationId = await AddParticipantAsync(meetId, athleteSlug, 80.5m);

        await InsertBanCoveringDateAsync(athleteSlug, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        await RecordAttemptAsync(recordClient, meetId, participationId, Discipline.Squat, 1, 100.0m);
        await RecordAttemptAsync(recordClient, meetId, participationId, Discipline.Bench, 1, 60.0m);
        await RecordAttemptAsync(recordClient, meetId, participationId, Discipline.Deadlift, 1, 150.0m);
        await channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{meetSlug}/participations",
                CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation? banned = participations.FirstOrDefault(p => p.ParticipationId == participationId);
        banned.ShouldNotBeNull();
        banned.Disqualified.ShouldBeTrue();
        banned.Total.ShouldBeGreaterThan(0m);

        // Cleanup
        recordClient.Dispose();
    }

    [Fact]
    public async Task BannedAthlete_IsExcludedFromRankings_WhileUnbannedAthleteIsRanked()
    {
        // Arrange
        (HttpClient recordClient, RecordComputationChannel channel) =
            fixture.CreateAuthorizedHttpClientWithRecordComputation();

        string bannedSlug = await CreateAthleteAsync("BanEx1", "m", new DateOnly(1988, 3, 10));
        string unbannedSlug = await CreateAthleteAsync("BanEx2", "m", new DateOnly(1985, 5, 20));
        _banAthleteSlugsToClear.Add(bannedSlug);

        (int meetId, string meetSlug) = await CreateMeetAndGetIdAsync(new DateOnly(2025, 5, 1));

        int bannedParticipationId = await AddParticipantAsync(meetId, bannedSlug, 80.5m);
        int unbannedParticipationId = await AddParticipantAsync(meetId, unbannedSlug, 82.0m);

        await InsertBanCoveringDateAsync(bannedSlug, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        // Banned athlete — valid lifts, but ban covers the meet date
        await RecordAttemptAsync(recordClient, meetId, bannedParticipationId, Discipline.Squat, 1, 120.0m);
        await RecordAttemptAsync(recordClient, meetId, bannedParticipationId, Discipline.Bench, 1, 70.0m);
        await RecordAttemptAsync(recordClient, meetId, bannedParticipationId, Discipline.Deadlift, 1, 160.0m);
        await channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Unbanned athlete — valid lifts, should rank 1
        await RecordAttemptAsync(recordClient, meetId, unbannedParticipationId, Discipline.Squat, 1, 80.0m);
        await RecordAttemptAsync(recordClient, meetId, unbannedParticipationId, Discipline.Bench, 1, 40.0m);
        await RecordAttemptAsync(recordClient, meetId, unbannedParticipationId, Discipline.Deadlift, 1, 120.0m);
        await channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<MeetParticipation>? participations = await _authorizedHttpClient
            .GetFromJsonAsync<IReadOnlyList<MeetParticipation>>(
                $"/meets/{meetSlug}/participations",
                CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation? banned = participations.FirstOrDefault(p => p.ParticipationId == bannedParticipationId);
        MeetParticipation? unbanned = participations.FirstOrDefault(p => p.ParticipationId == unbannedParticipationId);

        banned.ShouldNotBeNull();
        unbanned.ShouldNotBeNull();
        banned.Rank.ShouldBe(0);
        unbanned.Rank.ShouldBe(1);

        // Cleanup
        recordClient.Dispose();
    }

    private static async Task RecordAttemptAsync(
        HttpClient client,
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .Build();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender, DateOnly dateOfBirth)
    {
        string suffix = UniqueShortCode.Next();
        string firstName = $"{prefix}{suffix}";
        string lastName = "Bd";

        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender(gender)
            .WithDateOfBirth(dateOfBirth)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{firstName} {lastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<(int MeetId, string MeetSlug)> CreateMeetAndGetIdAsync(DateOnly startDate)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", CancellationToken.None);

        return (meetDetails!.MeetId, slug);
    }

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug, decimal bodyWeight)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = result!.ParticipationId;
        _participations.Add((meetId, participationId));
        return participationId;
    }

    private async Task InsertBanCoveringDateAsync(string athleteSlug, DateOnly fromDate, DateOnly toDate)
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            $"""
            DECLARE @aid INT = (SELECT AthleteId FROM Athletes WHERE Slug = '{athleteSlug}');
            INSERT INTO Bans (AthleteId, FromDate, ToDate) VALUES (@aid, '{fromDate:yyyy-MM-dd}', '{toDate:yyyy-MM-dd}');
            """,
            CancellationToken.None);
    }
}