using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using RecordEntity = KRAFT.Results.WebApi.Features.Records.Record;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

public sealed class ComputeRecordsTests(IntegrationTestFixture fixture)
{
    private const int SeedMeetId = 1;
    private const int RecordTestParticipationId = 100;
    private const int RecordTestAttemptId = 100;
    private const string AttemptWeightSql = "300.0";
    private const decimal AttemptWeight = 300.0m;

    [Fact]
    public async Task WhenGoodAttemptBeatsCurrentRecord_CreatesRecordAndCascades()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await SeedRecordComputationTestDataAsync(dbContext);

        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        // Act
        await service.ComputeRecordsAsync(RecordTestAttemptId, CancellationToken.None);

        // Assert — records should exist for full Masters4 cascade: masters4, masters3, masters2, masters1, open
        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.AttemptId == RecordTestAttemptId)
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
        createdRecords.ShouldAllBe(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg);
    }

    [Fact]
    public async Task WhenAttemptIsRecordedViaEndpoint_RecordIsCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClient();

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .WithCountryId(2)
            .Build();

        HttpResponseMessage athleteResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .Build();

        HttpResponseMessage participantResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantCommand,
            CancellationToken.None);

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearMastersCascadeRecordsAsync(dbContext);

        // Record bench and deadlift first so the participation has valid totals
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat that should trigger record computation via domain event
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);

        // Assert — records should exist for full Masters4 cascade: masters4, masters3, masters2, masters1, open
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
        HttpClient client = fixture.CreateAuthorizedHttpClient();

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .WithCountryId(2)
            .Build();

        HttpResponseMessage athleteResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .Build();

        HttpResponseMessage participantResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantCommand,
            CancellationToken.None);

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearMastersCascadeRecordsAsync(dbContext);

        // Record bench and deadlift first so the participation has valid totals
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

        // Record initial squat attempt (200kg) — records should be created at 200kg
        decimal initialWeight = 200.0m;
        await RecordAttempt(client, participationId, Discipline.Squat, 1, initialWeight);

        // Act — update the same squat attempt to 250kg
        decimal updatedWeight = 250.0m;
        await RecordAttempt(client, participationId, Discipline.Squat, 1, updatedWeight);

        // Assert — records should now reflect 250kg, not 200kg
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
        HttpClient client = fixture.CreateAuthorizedHttpClient();

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(Constants.BannedAthlete.Slug)
            .Build();

        HttpResponseMessage participantResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantCommand,
            CancellationToken.None);

        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        // Record bench and deadlift so total would be valid if not for the ban
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearMastersCascadeRecordsAsync(dbContext);

        // Act — record squat for banned athlete during ban period (meet date 2025-03-15)
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);

        // Assert — no records should be created for the banned athlete
        List<RecordEntity> createdRecords = await dbContext.Set<RecordEntity>()
            .Include(r => r.Attempt!)
                .ThenInclude(a => a.Participation)
            .Where(r => r.Attempt!.Participation.AthleteId == Constants.BannedAthlete.Id)
            .Where(r => r.IsCurrent)
            .ToListAsync(CancellationToken.None);

        createdRecords.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenRecordsPossibleIsFalse_NoRecordCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClient();

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .Build();

        HttpResponseMessage participantResponse = await client.PostAsJsonAsync(
            $"/meets/{Constants.NoRecordsMeet.Id}/participants",
            participantCommand,
            CancellationToken.None);

        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        // Record bench and deadlift so total would be valid
        await RecordAttemptForMeet(client, Constants.NoRecordsMeet.Id, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttemptForMeet(client, Constants.NoRecordsMeet.Id, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat at a meet where RecordsPossible = false
        await RecordAttemptForMeet(client, Constants.NoRecordsMeet.Id, participationId, Discipline.Squat, 1, AttemptWeight);

        // Assert — no records should be created
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
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
        HttpClient client = fixture.CreateAuthorizedHttpClient();

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .WithCountryId(2)
            .Build();

        HttpResponseMessage athleteResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .Build();

        HttpResponseMessage participantResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantCommand,
            CancellationToken.None);

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearMastersCascadeRecordsAsync(dbContext);

        // Act — record only squat (no bench or deadlift = no valid total for full powerlifting meet)
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);

        // Assert — no squat record should be created because there is no valid total
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
        HttpClient client = fixture.CreateAuthorizedHttpClient();

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .WithCountryId(2)
            .Build();

        HttpResponseMessage athleteResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .Build();

        HttpResponseMessage participantResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantCommand,
            CancellationToken.None);

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearMastersCascadeRecordsAsync(dbContext);

        // Record bench and deadlift first to establish valid total
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat with all 3 disciplines having good lifts
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);

        // Assert — squat record should be created with valid total
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

    private static async Task RecordAttempt(
        HttpClient client,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(true)
            .Build();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/meets/{SeedMeetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task RecordAttemptForMeet(
        HttpClient client,
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(true)
            .Build();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task ClearMastersCascadeRecordsAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            DELETE FROM Records
            WHERE AgeCategoryId IN (
                {TestSeedConstants.AgeCategory.OpenId},
                {TestSeedConstants.AgeCategory.Masters1Id},
                {TestSeedConstants.AgeCategory.Masters2Id},
                {TestSeedConstants.AgeCategory.Masters3Id},
                {TestSeedConstants.AgeCategory.Masters4Id})
            AND RecordCategoryId = 1
            AND IsRaw = 1
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedRecordComputationTestDataAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            -- Clear any existing records for masters age categories in raw squat 83kg to avoid interference
            DELETE FROM Records
            WHERE AgeCategoryId IN ({TestSeedConstants.AgeCategory.Masters1Id}, {TestSeedConstants.AgeCategory.Masters2Id}, {TestSeedConstants.AgeCategory.Masters3Id}, {TestSeedConstants.AgeCategory.Masters4Id})
            AND RecordCategoryId = 1
            AND IsRaw = 1
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg};

            -- Also clear the open raw squat 83kg record to ensure our attempt beats it
            DELETE FROM Records
            WHERE AgeCategoryId = {TestSeedConstants.AgeCategory.OpenId}
            AND RecordCategoryId = 1
            AND IsRaw = 1
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg};

            -- Participation in Masters4 age category for athlete 1 in test meet 1
            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({RecordTestParticipationId}, {TestSeedConstants.Athlete.Id}, {TestSeedConstants.Meet.Id}, 80.5, {TestSeedConstants.WeightCategory.Id83Kg}, {TestSeedConstants.AgeCategory.Masters4Id}, 1, 0, {AttemptWeightSql}, 130.0, 250.0, 680.0, 450.0, 95.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            -- Good squat attempt that beats any existing record
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({RecordTestAttemptId}, {RecordTestParticipationId}, 1, 1, {AttemptWeightSql}, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}