using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using RecordEntity = KRAFT.Results.WebApi.Features.Records.Record;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(ComputeRecordsTestsCollection))]
public sealed class ComputeRecordsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const decimal AttemptWeight = 300.0m;

    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;

    // Shared meet IDs (populated in InitializeAsync)
    private int _rawMeetId;
    private int _noRecordsMeetId;
    private int _deadliftMeetId;
    private int _benchMeetId;

    // Banned athlete slug (created in InitializeAsync)
    private string _bannedAthleteSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        // Create banned athlete via endpoint, then add ban via SQL (no ban endpoint exists)
        _bannedAthleteSlug = await CreateAthleteAsync("CmpBan", "m", new DateOnly(1990, 1, 1));

        await using AsyncServiceScope banScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext banDbContext = banScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        string banSql = $"""
            DECLARE @aid INT = (SELECT AthleteId FROM Athletes WHERE Slug = '{_bannedAthleteSlug}');
            INSERT INTO Bans (AthleteId, FromDate, ToDate) VALUES (@aid, '2020-01-01', '9999-12-31');
            """;

        await banDbContext.Database.ExecuteSqlRawAsync(banSql, TestContext.Current.CancellationToken);

        // Create shared meets via endpoints
        _rawMeetId = await CreateMeetAndGetIdAsync(isRaw: true);
        _noRecordsMeetId = await CreateMeetAndGetIdAsync(isRaw: true, recordsPossible: false);
        _deadliftMeetId = await CreateMeetAndGetIdAsync(isRaw: true, meetTypeId: 3);
        _benchMeetId = await CreateMeetAndGetIdAsync(isRaw: true, meetTypeId: 2);
    }

    public async ValueTask DisposeAsync()
    {
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
    public async Task WhenGoodAttemptBeatsCurrentRecord_CreatesRecordAndCascades()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id83Kg;

        string athleteSlug = await CreateAthleteAsync("CmpA1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        int squatAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Squat, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        // Act
        await service.ComputeRecordsAsync(squatAttemptId, CancellationToken.None);

        // Assert — records should exist for full Masters4 cascade: masters4, masters3, masters2, masters1, open
        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == squatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        List<string> cascadeSlugs = createdRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");

        createdRecords.ShouldAllBe(r => r.Weight == AttemptWeight);
        createdRecords.ShouldAllBe(r => r.EraId == TestSeedConstants.Era.CurrentId);
        createdRecords.ShouldAllBe(r => r.WeightCategoryId == weightCategoryId);
    }

    [Fact]
    public async Task WhenAttemptIsRecordedViaEndpoint_RecordIsCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpB1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        // Record bench and deadlift first so the participation has valid totals
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat that should trigger record computation via domain event
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — records should exist for full Masters4 cascade: masters4, masters3, masters2, masters1, open
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == AttemptWeight)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        List<string> cascadeSlugs = createdRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");
    }

    [Fact]
    public async Task WhenExistingAttemptIsUpdated_RecordIsRecomputed()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpC1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        // Record bench and deadlift first so the participation has valid totals
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 260.0m);

        // Record initial squat attempt (210kg) — records should be created at 210kg
        decimal initialWeight = 210.0m;
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, initialWeight);

        // Act — update the same squat attempt to 260kg
        decimal updatedWeight = 260.0m;
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, updatedWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — records should now reflect 260kg, not 210kg
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == updatedWeight)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        List<string> cascadeSlugs = createdRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");
    }

    [Fact]
    public async Task WhenAthleteIsBanned_NoRecordCreated()
    {
        // Arrange
        int participationId = await AddParticipantAsync(_rawMeetId, _bannedAthleteSlug, 80.5m);

        // Record bench and deadlift so total would be valid if not for the ban
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat for banned athlete during ban period
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — no records should be created for the banned athlete
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int athleteId = await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationId)
            .Select(p => p.AthleteId)
            .SingleAsync(CancellationToken.None);

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Include(r => r.Attempt!)
                .ThenInclude(a => a.Participation)
            .Where(r => r.Attempt!.Participation.AthleteId == athleteId)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        createdRecords.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenRecordsPossibleIsFalse_NoRecordCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpD1", "m", new DateOnly(1950, 1, 1), countryCode: "NOR");
        int participationId = await AddParticipantAsync(_noRecordsMeetId, athleteSlug, 80.5m);

        // Record bench and deadlift so total would be valid
        await RecordAttemptAsync(_noRecordsMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_noRecordsMeetId, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat at a meet where RecordsPossible = false
        await RecordAttemptAsync(_noRecordsMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — no records should be created
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Include(r => r.Attempt!)
                .ThenInclude(a => a.Participation)
            .Where(r => r.Attempt!.Participation.ParticipationId == participationId)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        createdRecords.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenNoValidTotal_NoRecordCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpE1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        // Act — record only squat (no bench or deadlift = no valid total for full powerlifting meet)
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — no squat record should be created because there is no valid total
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Include(r => r.Attempt!)
                .ThenInclude(a => a.Participation)
            .Where(r => r.Attempt!.Participation.ParticipationId == participationId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        createdRecords.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenValidTotalExists_RecordIsCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpF1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        // Record bench and deadlift first to establish valid total
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat with all 3 disciplines having good lifts
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — squat record should be created with valid total
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Include(r => r.Attempt!)
                .ThenInclude(a => a.Participation)
            .Where(r => r.Attempt!.Participation.ParticipationId == participationId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.Weight == AttemptWeight)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        List<string> cascadeSlugs = createdRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");
    }

    [Fact]
    public async Task WhenMasters1AthleteCompetesAsOpen_Masters1RecordIsCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpG1", "m", new DateOnly(1984, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m, ageCategorySlug: "open");

        // Record bench and deadlift first so the participation has valid totals
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat that should trigger record computation
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — records should cascade for biological Masters1: masters1 + open
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == AttemptWeight)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        List<string> cascadeSlugs = createdRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_BenchRecordIsAlsoCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpH1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        // Record squat, bench, then deadlift (deadlift triggers the last event)
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 210.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 140.0m);

        // Act — deadlift completes the total, enabling all records
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — bench record should exist (not just the triggering deadlift)
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> benchRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Bench)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 140.0m)
            .ToListAsync(CancellationToken.None);

        benchRecords.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_SquatRecordIsAlsoCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpI1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        // Record squat first (no valid total yet), then bench, then deadlift
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 210.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 140.0m);

        // Act — deadlift completes the total, enabling all records
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — squat record should exist even though it was recorded before total was valid
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> squatRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 210.0m)
            .ToListAsync(CancellationToken.None);

        squatRecords.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_TotalRecordIsCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpJ1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 210.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 140.0m);

        // Act — deadlift completes the total
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — total record should exist with weight = 210 + 140 + 260 = 610
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> totalRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 610.0m)
            .ToListAsync(CancellationToken.None);

        totalRecords.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task WhenDeadliftRecordedAtDeadliftOnlyMeet_DeadliftSingleRecordIsCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpK1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_deadliftMeetId, athleteSlug, 80.5m);

        // Act — record a good deadlift at the deadlift-only meet
        await RecordAttemptAsync(_deadliftMeetId, participationId, Discipline.Deadlift, 1, 280.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — DeadliftSingle record should exist
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> deadliftSingleRecords = await dbContext.Set<RecordEntity>()
            .Include(r => r.Attempt!)
                .ThenInclude(a => a.Participation)
            .Where(r => r.Attempt!.Participation.ParticipationId == participationId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.DeadliftSingle)
            .ToListAsync(CancellationToken.None);

        deadliftSingleRecords.ShouldNotBeEmpty();
        deadliftSingleRecords.ShouldAllBe(r => r.Weight == 280.0m);
    }

    [Fact]
    public async Task WhenAthleteIsNotIcelandic_NoRecordCreated()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpL1", "m", new DateOnly(1950, 1, 1), countryCode: "NOR");
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 80.5m);

        // Record bench and deadlift so total would be valid
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat for non-Icelandic athlete
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, AttemptWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — no records should be created for a non-Icelandic athlete
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Include(r => r.Attempt!)
                .ThenInclude(a => a.Participation)
            .Where(r => r.Attempt!.Participation.ParticipationId == participationId)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        createdRecords.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenTwoLiftersBreakSameRecordInSameMeet_EarlierAttemptWins()
    {
        // Arrange
        string athleteASlug = await CreateAthleteAsync("CmpM1", "m", new DateOnly(1950, 1, 1));
        string athleteBSlug = await CreateAthleteAsync("CmpM2", "m", new DateOnly(1950, 1, 1));

        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, 80.5m);
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, 80.5m);

        // Give both athletes valid totals
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 260.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 260.0m);

        // Athlete A squats 210kg
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 210.0m);

        // Act — Athlete B squats 220kg (heavier)
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 220.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — the current record should belong to Athlete B at 220kg
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> currentRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 220.0m)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        List<string> cascadeSlugs = currentRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");
    }

    [Fact]
    public async Task WhenSecondLifterDoesNotBeatExistingRecord_NoNewRecord()
    {
        // Arrange
        string athleteASlug = await CreateAthleteAsync("CmpN1", "m", new DateOnly(1950, 1, 1));
        string athleteBSlug = await CreateAthleteAsync("CmpN2", "m", new DateOnly(1950, 1, 1));

        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, 80.5m);
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, 80.5m);

        // Give both athletes valid totals
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 260.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 260.0m);

        // Athlete A squats 210kg — establishes the record
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 210.0m);

        // Act — Athlete B squats 205kg (less than A's 210kg)
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 205.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Assert — current record should still belong to Athlete A at 210kg
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> currentRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        currentRecords.ShouldAllBe(r => r.Weight == 210.0m);

        List<string> cascadeSlugs = currentRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");

        // Athlete B's 205kg squat is a valid historical record (beaten by A's 210kg)
        // but should not be marked as current
        List<RecordEntity> athleteBRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.Weight == 205.0m)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.IsRaw)
            .ToListAsync(CancellationToken.None);

        athleteBRecords.Count.ShouldBe(5);
        athleteBRecords.ShouldAllBe(r => !r.IsCurrent);
    }

    [Fact]
    public async Task WhenAttemptMarkedNoGood_AllRecordsRevoked_SlotRebuilt()
    {
        // Arrange
        string athleteSlug = await CreateAthleteAsync("CmpO1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        int squatAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Squat, TestContext.Current.CancellationToken);
        int benchAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Bench, TestContext.Current.CancellationToken);
        int deadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Deadlift, TestContext.Current.CancellationToken);

        List<int> attemptIds = [squatAttemptId, benchAttemptId, deadliftAttemptId];

        List<RecordEntity> recordsBefore = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId != null)
            .Where(r => attemptIds.Contains(r.AttemptId!.Value))
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        recordsBefore.ShouldNotBeEmpty();

        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == squatAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Good, false),
                CancellationToken.None);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Squat, 0m)
                      .SetProperty(p => p.Total, 0m),
                CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        // Act
        await service.ComputeRecordsAsync(squatAttemptId, CancellationToken.None);

        // Assert
        List<RecordEntity> remainingRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId != null)
            .Where(r => attemptIds.Contains(r.AttemptId!.Value))
            .ToListAsync(CancellationToken.None);

        // Squat, Bench, Deadlift, and Total records should be revoked (no valid total).
        // BenchSingle and DeadliftSingle survive because single-lift records don't
        // require a valid total — 2 categories x 5 age categories = 10 records.
        remainingRecords.Count.ShouldBe(10);
        remainingRecords.ShouldAllBe(r =>
            r.RecordCategoryId == RecordCategory.BenchSingle ||
            r.RecordCategoryId == RecordCategory.DeadliftSingle);
    }

    [Fact]
    public async Task WhenNoGoodOverturned_RecordsReEvaluated()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteSlug = await CreateAthleteAsync("CmpP1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 200.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        int squatAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Squat, TestContext.Current.CancellationToken);
        int benchAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Bench, TestContext.Current.CancellationToken);
        int deadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Deadlift, TestContext.Current.CancellationToken);

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        // Start with deadlift as a bad attempt — no valid total yet
        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == deadliftAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Good, false),
                TestContext.Current.CancellationToken);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Deadlift, 0m)
                      .SetProperty(p => p.Total, 0m),
                TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(deadliftAttemptId, CancellationToken.None);

        List<int> attemptIds = [squatAttemptId, benchAttemptId, deadliftAttemptId];

        List<RecordEntity> recordsBeforeOverturn = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId != null)
            .Where(r => attemptIds.Contains(r.AttemptId!.Value))
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        recordsBeforeOverturn.ShouldBeEmpty();

        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == deadliftAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Good, true)
                      .SetProperty(a => a.Weight, 200m),
                CancellationToken.None);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Deadlift, 200m)
                      .SetProperty(p => p.Total, 530m),
                CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        // Act
        await service.ComputeRecordsAsync(deadliftAttemptId, CancellationToken.None);

        // Assert
        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId != null)
            .Where(r => attemptIds.Contains(r.AttemptId!.Value))
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        List<RecordCategory> recordCategories = createdRecords
            .Select(r => r.RecordCategoryId)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        recordCategories.ShouldContain(RecordCategory.Squat);
        recordCategories.ShouldContain(RecordCategory.Bench);
        recordCategories.ShouldContain(RecordCategory.Deadlift);
        recordCategories.ShouldContain(RecordCategory.Total);

        createdRecords.ShouldAllBe(r => r.IsCurrent);
    }

    [Fact]
    public async Task WhenAttemptWeightReduced_RecordRevoked_PreviousHolderRestored()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteASlug = await CreateAthleteAsync("CmpQ1", "m", new DateOnly(1950, 1, 1));
        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        int aSquatAttemptId = await GetAttemptIdAsync(
            dbContext, participationAId, Discipline.Squat, TestContext.Current.CancellationToken);

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(aSquatAttemptId, CancellationToken.None);

        List<RecordEntity> recordsAfterA = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == aSquatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        recordsAfterA.ShouldNotBeEmpty();

        string athleteBSlug = await CreateAthleteAsync("CmpQ2", "m", new DateOnly(1950, 1, 1));
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 210.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        int bSquatAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Squat, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(bSquatAttemptId, CancellationToken.None);

        List<RecordEntity> recordsAfterB = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == bSquatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        recordsAfterB.ShouldNotBeEmpty();

        // Reduce B's squat below A's weight — total stays valid (all disciplines still good)
        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == bSquatAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Weight, 190m),
                CancellationToken.None);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationBId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Squat, 190m)
                      .SetProperty(p => p.Total, 570m),
                CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        // Act
        await service.ComputeRecordsAsync(bSquatAttemptId, CancellationToken.None);

        // Assert
        List<RecordEntity> bCurrentSquatRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == bSquatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        bCurrentSquatRecords.ShouldBeEmpty();

        List<RecordEntity> aRestoredRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == aSquatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        aRestoredRecords.ShouldNotBeEmpty();
        aRestoredRecords.ShouldAllBe(r => r.Weight == 200m);
    }

    [Fact]
    public async Task WhenAttemptWeightCorrected_SameAttemptId_RecordWeightUpdated()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteSlug = await CreateAthleteAsync("CmpR1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_rawMeetId, athleteSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        int squatAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Squat, TestContext.Current.CancellationToken);

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(squatAttemptId, CancellationToken.None);

        List<RecordEntity> initialRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == squatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        initialRecords.ShouldNotBeEmpty();
        initialRecords.ShouldAllBe(r => r.Weight == 200m);

        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == squatAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Weight, 210m),
                CancellationToken.None);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Squat, 210m)
                      .SetProperty(p => p.Total, 590m),
                CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        // Act
        await service.ComputeRecordsAsync(squatAttemptId, CancellationToken.None);

        // Assert
        List<RecordEntity> oldWeightRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == squatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.Weight == 200m)
            .ToListAsync(CancellationToken.None);

        oldWeightRecords.ShouldBeEmpty();

        List<RecordEntity> updatedRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == squatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.Weight == 210m)
            .ToListAsync(CancellationToken.None);

        updatedRecords.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task WhenTotalReduced_TotalRecordRevoked_SlotRebuilt()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteASlug = await CreateAthleteAsync("CmpS1", "m", new DateOnly(1950, 1, 1));
        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 150.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        int aDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationAId, Discipline.Deadlift, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(aDeadliftAttemptId, CancellationToken.None);

        List<RecordEntity> aTotalRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == aDeadliftAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .ToListAsync(CancellationToken.None);

        aTotalRecords.ShouldNotBeEmpty();

        string athleteBSlug = await CreateAthleteAsync("CmpS2", "m", new DateOnly(1950, 1, 1));
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 250.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 150.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 300.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        int bDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Deadlift, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(bDeadliftAttemptId, CancellationToken.None);

        List<RecordEntity> bTotalRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == bDeadliftAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .ToListAsync(CancellationToken.None);

        bTotalRecords.ShouldNotBeEmpty();

        // Reduce B's deadlift so total drops below A's — all disciplines stay good
        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == bDeadliftAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Weight, 100m),
                CancellationToken.None);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationBId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Deadlift, 100m)
                      .SetProperty(p => p.Total, 500m),
                CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        // Act
        await service.ComputeRecordsAsync(bDeadliftAttemptId, CancellationToken.None);

        // Assert
        List<RecordEntity> bFinalTotalRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == bDeadliftAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .ToListAsync(CancellationToken.None);

        bFinalTotalRecords.ShouldBeEmpty();

        List<RecordEntity> aRestoredTotalRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == aDeadliftAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .ToListAsync(CancellationToken.None);

        aRestoredTotalRecords.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task WhenThreeAthletesProgressivelySetTotalRecord_AllPreservedInHistory()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteASlug = await CreateAthleteAsync("CmpT1", "m", new DateOnly(1950, 1, 1));
        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 210.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        string athleteBSlug = await CreateAthleteAsync("CmpT2", "m", new DateOnly(1950, 1, 1));
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 210.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 270.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        string athleteCSlug = await CreateAthleteAsync("CmpT3", "m", new DateOnly(1950, 1, 1));
        int participationCId = await AddParticipantAsync(_rawMeetId, athleteCSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationCId, Discipline.Squat, 1, 210.0m);
        await RecordAttemptAsync(_rawMeetId, participationCId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationCId, Discipline.Deadlift, 1, 280.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        int aDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationAId, Discipline.Deadlift, TestContext.Current.CancellationToken);
        int bDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Deadlift, TestContext.Current.CancellationToken);
        int cDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationCId, Discipline.Deadlift, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        // Compute records for A (total=610), then B (total=620), then C (total=630)
        await service.ComputeRecordsAsync(aDeadliftAttemptId, CancellationToken.None);
        await service.ComputeRecordsAsync(bDeadliftAttemptId, CancellationToken.None);

        // Act
        await service.ComputeRecordsAsync(cDeadliftAttemptId, CancellationToken.None);

        // Assert — all 3 total records should be preserved in history for the open cascade slot
        List<RecordEntity> totalRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.AgeCategoryId == TestSeedConstants.AgeCategory.OpenId)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .Where(r => r.IsRaw)
            .OrderBy(r => r.Weight)
            .ToListAsync(CancellationToken.None);

        totalRecords.Count.ShouldBe(3);
        totalRecords[0].Weight.ShouldBe(610m);
        totalRecords[0].IsCurrent.ShouldBeFalse();
        totalRecords[1].Weight.ShouldBe(620m);
        totalRecords[1].IsCurrent.ShouldBeFalse();
        totalRecords[2].Weight.ShouldBe(630m);
        totalRecords[2].IsCurrent.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenParticipantRemoved_RecordsRevoked_SlotsRebuilt()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteASlug = await CreateAthleteAsync("CmpU1", "m", new DateOnly(1950, 1, 1));
        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 211.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        int aSquatAttemptId = await GetAttemptIdAsync(
            dbContext, participationAId, Discipline.Squat, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(aSquatAttemptId, CancellationToken.None);

        List<RecordEntity> recordsAfterA = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == aSquatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        recordsAfterA.ShouldNotBeEmpty();

        // Seed athlete B with squat=220 (beats A's 211)
        string athleteBSlug = await CreateAthleteAsync("CmpU2", "m", new DateOnly(1950, 1, 1));
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 220.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        int bSquatAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Squat, TestContext.Current.CancellationToken);
        int bBenchAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Bench, TestContext.Current.CancellationToken);
        int bDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Deadlift, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(bSquatAttemptId, CancellationToken.None);

        List<RecordEntity> bRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == bSquatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        bRecords.ShouldNotBeEmpty();

        // Act — simulate participant B removal: collect affected slots, delete records + participation, rebuild
        List<int> bAttemptIds = [bSquatAttemptId, bBenchAttemptId, bDeadliftAttemptId];

        List<SlotKey> affectedSlots = await dbContext.Set<RecordEntity>()
            .AsNoTracking()
            .Where(r => r.AttemptId != null)
            .Where(r => bAttemptIds.Contains(r.AttemptId!.Value))
            .Select(r => new SlotKey(
                r.EraId, r.AgeCategoryId, r.WeightCategoryId, r.RecordCategoryId, r.IsRaw))
            .Distinct()
            .ToListAsync(CancellationToken.None);

        string deleteBSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({bSquatAttemptId}, {bBenchAttemptId}, {bDeadliftAttemptId});
            DELETE FROM Attempts WHERE AttemptId IN ({bSquatAttemptId}, {bBenchAttemptId}, {bDeadliftAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {participationBId};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(deleteBSql, TestContext.Current.CancellationToken);

        // Remove B from tracked participations so DisposeAsync doesn't try to delete it again
        _participations.Remove((_rawMeetId, participationBId));

        dbContext.ChangeTracker.Clear();

        await service.RebuildSlotsAsync(affectedSlots, CancellationToken.None);

        // Assert — B's records should be revoked
        List<RecordEntity> bRemainingRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId != null)
            .Where(r => bAttemptIds.Contains(r.AttemptId!.Value))
            .ToListAsync(CancellationToken.None);

        bRemainingRecords.ShouldBeEmpty();

        // A's squat record should be restored
        List<RecordEntity> aRestoredRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == aSquatAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .ToListAsync(CancellationToken.None);

        aRestoredRecords.ShouldNotBeEmpty();
        aRestoredRecords.ShouldAllBe(r => r.Weight == 211m);
    }

    [Fact]
    public async Task WhenBenchOnlyMeet_AttemptNoGood_RecordRevoked()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteSlug = await CreateAthleteAsync("CmpV1", "m", new DateOnly(1950, 1, 1));
        int participationId = await AddParticipantAsync(_benchMeetId, athleteSlug, 90.0m);

        await RecordAttemptAsync(_benchMeetId, participationId, Discipline.Bench, 1, 150.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        int benchAttemptId = await GetAttemptIdAsync(
            dbContext, participationId, Discipline.Bench, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        await service.ComputeRecordsAsync(benchAttemptId, CancellationToken.None);

        List<RecordEntity> benchRecordsBefore = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == benchAttemptId)
            .Where(r => r.IsCurrent)
            .Where(r => r.RecordCategoryId == RecordCategory.BenchSingle)
            .ToListAsync(CancellationToken.None);

        benchRecordsBefore.ShouldNotBeEmpty();

        // Mark attempt as no good
        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == benchAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Good, false),
                CancellationToken.None);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == participationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Benchpress, 0m)
                      .SetProperty(p => p.Total, 0m),
                CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        // Act
        await service.ComputeRecordsAsync(benchAttemptId, CancellationToken.None);

        // Assert — no records should remain for this attempt
        List<RecordEntity> benchRecordsAfter = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == benchAttemptId)
            .ToListAsync(CancellationToken.None);

        benchRecordsAfter.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenBackfillRunsAfterComputation_NoChanges()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string athleteASlug = await CreateAthleteAsync("CmpW1", "m", new DateOnly(1950, 1, 1));
        int participationAId = await AddParticipantAsync(_rawMeetId, athleteASlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, participationAId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        string athleteBSlug = await CreateAthleteAsync("CmpW2", "m", new DateOnly(1950, 1, 1));
        int participationBId = await AddParticipantAsync(_rawMeetId, athleteBSlug, 90.0m);

        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Squat, 1, 220.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Bench, 1, 140.0m);
        await RecordAttemptAsync(_rawMeetId, participationBId, Discipline.Deadlift, 1, 260.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        int aSquatAttemptId = await GetAttemptIdAsync(
            dbContext, participationAId, Discipline.Squat, TestContext.Current.CancellationToken);
        int aBenchAttemptId = await GetAttemptIdAsync(
            dbContext, participationAId, Discipline.Bench, TestContext.Current.CancellationToken);
        int aDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationAId, Discipline.Deadlift, TestContext.Current.CancellationToken);
        int bSquatAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Squat, TestContext.Current.CancellationToken);
        int bBenchAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Bench, TestContext.Current.CancellationToken);
        int bDeadliftAttemptId = await GetAttemptIdAsync(
            dbContext, participationBId, Discipline.Deadlift, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        // Compute records for A's attempts, then B's
        await service.ComputeRecordsAsync(aSquatAttemptId, CancellationToken.None);
        await service.ComputeRecordsAsync(aBenchAttemptId, CancellationToken.None);
        await service.ComputeRecordsAsync(aDeadliftAttemptId, CancellationToken.None);
        await service.ComputeRecordsAsync(bSquatAttemptId, CancellationToken.None);
        await service.ComputeRecordsAsync(bBenchAttemptId, CancellationToken.None);
        await service.ComputeRecordsAsync(bDeadliftAttemptId, CancellationToken.None);

        // Snapshot records before backfill
        List<RecordEntity> recordsBefore = await dbContext.Set<RecordEntity>()
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.IsRaw)
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .AsNoTracking()
            .ToListAsync(CancellationToken.None);

        List<int> recordIdsBefore = recordsBefore
            .Select(r => r.RecordId)
            .OrderBy(id => id)
            .ToList();

        Dictionary<int, decimal> weightsBefore = recordsBefore
            .ToDictionary(r => r.RecordId, r => r.Weight);

        Dictionary<int, bool> isCurrentBefore = recordsBefore
            .ToDictionary(r => r.RecordId, r => r.IsCurrent);

        // Act — run backfill
        IServiceScopeFactory scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        using BackfillRecordsJob job = new(scopeFactory, NullLogger<BackfillRecordsJob>.Instance);

        await job.StartAsync(CancellationToken.None);
        await (job.ExecuteTask ?? Task.CompletedTask);

        // Assert — records should be identical after backfill
        await using AsyncServiceScope assertScope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        List<RecordEntity> recordsAfter = await assertDb.Set<RecordEntity>()
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.IsRaw)
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .AsNoTracking()
            .ToListAsync(CancellationToken.None);

        List<int> recordIdsAfter = recordsAfter
            .Select(r => r.RecordId)
            .OrderBy(id => id)
            .ToList();

        // No records added or deleted
        recordIdsAfter.Count.ShouldBe(recordIdsBefore.Count);
        recordIdsAfter.ShouldBe(recordIdsBefore);

        // No weights or IsCurrent flags changed
        foreach (RecordEntity record in recordsAfter)
        {
            weightsBefore[record.RecordId].ShouldBe(record.Weight);
            isCurrentBefore[record.RecordId].ShouldBe(record.IsCurrent);
        }
    }

    [Fact]
    public async Task WhenNonIcelandicAthleteHasBetterLift_SlotRebuildIgnoresNonIcelander()
    {
        // Arrange
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id63Kg;

        string norwegianSlug = await CreateAthleteAsync("CmpX1", "f", new DateOnly(1950, 1, 1), countryCode: "NOR");
        int norwegianParticipationId = await AddParticipantAsync(_rawMeetId, norwegianSlug, 60.0m);

        await RecordAttemptAsync(_rawMeetId, norwegianParticipationId, Discipline.Squat, 1, 250.0m);
        await RecordAttemptAsync(_rawMeetId, norwegianParticipationId, Discipline.Bench, 1, 150.0m);
        await RecordAttemptAsync(_rawMeetId, norwegianParticipationId, Discipline.Deadlift, 1, 300.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        string icelandicSlug = await CreateAthleteAsync("CmpX2", "f", new DateOnly(1950, 1, 1));
        int icelandicParticipationId = await AddParticipantAsync(_rawMeetId, icelandicSlug, 60.0m);

        await RecordAttemptAsync(_rawMeetId, icelandicParticipationId, Discipline.Squat, 1, 200.0m);
        await RecordAttemptAsync(_rawMeetId, icelandicParticipationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptAsync(_rawMeetId, icelandicParticipationId, Discipline.Deadlift, 1, 250.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        int icelandicSquatAttemptId = await GetAttemptIdAsync(
            dbContext, icelandicParticipationId, Discipline.Squat, TestContext.Current.CancellationToken);
        int norwegianSquatAttemptId = await GetAttemptIdAsync(
            dbContext, norwegianParticipationId, Discipline.Squat, TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        // Act — compute records triggered by the Icelandic athlete's squat
        await service.ComputeRecordsAsync(icelandicSquatAttemptId, CancellationToken.None);

        // Assert — the current squat record must belong to the Icelandic athlete (200kg),
        // not the Norwegian athlete who lifted heavier (250kg)
        List<RecordEntity> currentSquatRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.EraId == TestSeedConstants.Era.CurrentId)
            .Where(r => r.WeightCategoryId == weightCategoryId)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.IsRaw)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        currentSquatRecords.ShouldNotBeEmpty();
        currentSquatRecords.ShouldAllBe(r => r.AttemptId == icelandicSquatAttemptId);
        currentSquatRecords.ShouldAllBe(r => r.Weight == 200m);

        List<RecordEntity> norwegianRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == norwegianSquatAttemptId)
            .ToListAsync(CancellationToken.None);

        norwegianRecords.ShouldBeEmpty();
    }

    private static async Task<int> GetAttemptIdAsync(
        ResultsDbContext dbContext,
        int participationId,
        Discipline discipline,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Attempt>()
            .Where(a => a.ParticipationId == participationId)
            .Where(a => a.Discipline == discipline)
            .Where(a => a.Round == 1)
            .Select(a => a.AttemptId)
            .SingleAsync(cancellationToken);
    }

    private async Task<string> CreateAthleteAsync(
        string prefix, string gender, DateOnly dateOfBirth, string countryCode = "ISL")
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Cr";

        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender(gender)
            .WithDateOfBirth(dateOfBirth)
            .WithCountryCode(countryCode)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{firstName} {lastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<int> CreateMeetAndGetIdAsync(
        bool isRaw, int? meetTypeId = null, bool recordsPossible = true)
    {
        CreateMeetCommandBuilder builder = new CreateMeetCommandBuilder()
            .WithIsRaw(isRaw)
            .WithRecordsPossible(recordsPossible);

        if (meetTypeId.HasValue)
        {
            builder.WithMeetTypeId(meetTypeId.Value);
        }

        CreateMeetCommand command = builder.Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", CancellationToken.None);

        return meetDetails!.MeetId;
    }

    private async Task<int> AddParticipantAsync(
        int meetId, string athleteSlug, decimal bodyWeight, string? ageCategorySlug = null)
    {
        AddParticipantCommandBuilder builder = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight);

        if (ageCategorySlug is not null)
        {
            builder.WithAgeCategorySlug(ageCategorySlug);
        }

        AddParticipantCommand command = builder.Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = result!.ParticipationId;
        _participations.Add((meetId, participationId));
        return participationId;
    }

    private async Task RecordAttemptAsync(
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight,
        bool good = true)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(good)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }
}