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
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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

        try
        {
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
        finally
        {
            await CleanupDirectSeedTestDataAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenAttemptIsRecordedViaEndpoint_RecordIsCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
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

        await ClearAllRecordCategoriesAsync(dbContext);

        // Record bench and deadlift first so the participation has valid totals
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat that should trigger record computation via domain event
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);
        await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

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
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await CleanupStaleTestDataFor83KgAsync(dbContext);

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
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

        await ClearAllRecordCategoriesAsync(dbContext);

        try
        {
            // Record bench and deadlift first so the participation has valid totals
            await RecordAttempt(client, participationId, Discipline.Bench, 1, 140.0m);
            await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 260.0m);

            // Record initial squat attempt (210kg) — records should be created at 210kg
            decimal initialWeight = 210.0m;
            await RecordAttempt(client, participationId, Discipline.Squat, 1, initialWeight);

            // Act — update the same squat attempt to 260kg
            decimal updatedWeight = 260.0m;
            await RecordAttempt(client, participationId, Discipline.Squat, 1, updatedWeight);
            await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

            // Assert — records should now reflect 260kg, not 210kg
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
        finally
        {
            await CleanupEndpointTestParticipationsAsync(dbContext, participationId);
            await ClearAllRecordCategoriesAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenAthleteIsBanned_NoRecordCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

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

        await ClearAllRecordCategoriesAsync(dbContext);

        // Act — record squat for banned athlete during ban period (meet date 2025-03-15)
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);
        await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

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
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
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
        await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

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
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

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

        await ClearAllRecordCategoriesAsync(dbContext);

        // Act — record only squat (no bench or deadlift = no valid total for full powerlifting meet)
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);
        await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

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
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
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

        await ClearAllRecordCategoriesAsync(dbContext);

        // Record bench and deadlift first to establish valid total
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

        // Act — record squat with all 3 disciplines having good lifts
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);
        await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task WhenMasters1AthleteCompetesAsOpen_Masters1RecordIsCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await CleanupStaleTestDataFor83KgAsync(dbContext);

        DateOnly masters1DateOfBirth = new(1984, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters1DateOfBirth)
            .Build();

        HttpResponseMessage athleteResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteCommand,
            CancellationToken.None);

        athleteResponse.EnsureSuccessStatusCode();

        string athleteSlug = Slug.Create($"{athleteCommand.FirstName} {athleteCommand.LastName}");

        AddParticipantCommand participantCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithAgeCategorySlug("open")
            .Build();

        HttpResponseMessage participantResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantCommand,
            CancellationToken.None);

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        await ClearAllRecordCategoriesAsync(dbContext);

        try
        {
            // Record bench and deadlift first so the participation has valid totals
            await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
            await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

            // Act — record squat that should trigger record computation
            await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);
            await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

            // Assert — records should cascade for biological Masters1: masters1 + open
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
        finally
        {
            await CleanupEndpointTestParticipationsAsync(dbContext, participationId);
            await ClearAllRecordCategoriesAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_BenchRecordIsAlsoCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await CleanupStaleTestDataFor83KgAsync(dbContext);

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
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

        await ClearAllRecordCategoriesAsync(dbContext);

        try
        {
            // Record squat, bench, then deadlift (deadlift triggers the last event)
            await RecordAttempt(client, participationId, Discipline.Squat, 1, 210.0m);
            await RecordAttempt(client, participationId, Discipline.Bench, 1, 140.0m);

            // Act — deadlift completes the total, enabling all records
            await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 260.0m);
            await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

            // Assert — bench record should exist (not just the triggering deadlift)
            List<RecordEntity> benchRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.IsCurrent)
                .Where(r => r.IsRaw)
                .Where(r => r.RecordCategoryId == RecordCategory.Bench)
                .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
                .Where(r => r.Weight == 140.0m)
                .ToListAsync(CancellationToken.None);

            benchRecords.ShouldNotBeEmpty();
        }
        finally
        {
            await CleanupEndpointTestParticipationsAsync(dbContext, participationId);
            await ClearAllRecordCategoriesAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_SquatRecordIsAlsoCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await CleanupStaleTestDataFor83KgAsync(dbContext);

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
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

        await ClearAllRecordCategoriesAsync(dbContext);

        try
        {
            // Record squat first (no valid total yet), then bench, then deadlift
            await RecordAttempt(client, participationId, Discipline.Squat, 1, 210.0m);
            await RecordAttempt(client, participationId, Discipline.Bench, 1, 140.0m);

            // Act — deadlift completes the total, enabling all records
            await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 260.0m);
            await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

            // Assert — squat record should exist even though it was recorded before total was valid
            List<RecordEntity> squatRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.IsCurrent)
                .Where(r => r.IsRaw)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
                .Where(r => r.Weight == 210.0m)
                .ToListAsync(CancellationToken.None);

            squatRecords.ShouldNotBeEmpty();
        }
        finally
        {
            await CleanupEndpointTestParticipationsAsync(dbContext, participationId);
            await ClearAllRecordCategoriesAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_TotalRecordIsCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await CleanupStaleTestDataFor83KgAsync(dbContext);

        DateOnly masters4DateOfBirth = new(1950, 1, 1);
        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
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

        await ClearAllRecordCategoriesAsync(dbContext);

        try
        {
            await RecordAttempt(client, participationId, Discipline.Squat, 1, 210.0m);
            await RecordAttempt(client, participationId, Discipline.Bench, 1, 140.0m);

            // Act — deadlift completes the total
            await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 260.0m);
            await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

            // Assert — total record should exist with weight = 210 + 140 + 260 = 610
            List<RecordEntity> totalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.IsCurrent)
                .Where(r => r.IsRaw)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
                .Where(r => r.Weight == 610.0m)
                .ToListAsync(CancellationToken.None);

            totalRecords.ShouldNotBeEmpty();
        }
        finally
        {
            await CleanupEndpointTestParticipationsAsync(dbContext, participationId);
            await ClearAllRecordCategoriesAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenDeadliftRecordedAtDeadliftOnlyMeet_DeadliftSingleRecordIsCreated()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        CreateAthleteCommand athleteCommand = new CreateAthleteCommandBuilder().Build();

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
            $"/meets/{Constants.DeadliftMeet.Id}/participants",
            participantCommand,
            CancellationToken.None);

        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        // Act — record a good deadlift at the deadlift-only meet
        await RecordAttemptForMeet(
            client,
            Constants.DeadliftMeet.Id,
            participationId,
            Discipline.Deadlift,
            1,
            280.0m);
        await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

        // Assert — DeadliftSingle record should exist
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
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
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

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

        participantResponse.EnsureSuccessStatusCode();

        AddParticipantResponse? participantResult = await participantResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationId = participantResult!.ParticipationId;

        // Record bench and deadlift so total would be valid
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 250.0m);

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearAllRecordCategoriesAsync(dbContext);

        // Act — record squat for non-Icelandic athlete
        await RecordAttempt(client, participationId, Discipline.Squat, 1, AttemptWeight);
        await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

        // Assert — no records should be created for a non-Icelandic athlete
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
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await CleanupStaleTestDataFor83KgAsync(dbContext);

        DateOnly masters4DateOfBirth = new(1950, 1, 1);

        CreateAthleteCommand athleteACommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .Build();

        HttpResponseMessage athleteAResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteACommand,
            CancellationToken.None);

        athleteAResponse.EnsureSuccessStatusCode();

        string athleteASlug = Slug.Create($"{athleteACommand.FirstName} {athleteACommand.LastName}");

        CreateAthleteCommand athleteBCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .Build();

        HttpResponseMessage athleteBResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteBCommand,
            CancellationToken.None);

        athleteBResponse.EnsureSuccessStatusCode();

        string athleteBSlug = Slug.Create($"{athleteBCommand.FirstName} {athleteBCommand.LastName}");

        AddParticipantCommand participantACommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteASlug)
            .Build();

        HttpResponseMessage participantAResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantACommand,
            CancellationToken.None);

        AddParticipantResponse? participantAResult = await participantAResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationAId = participantAResult!.ParticipationId;

        AddParticipantCommand participantBCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteBSlug)
            .Build();

        HttpResponseMessage participantBResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantBCommand,
            CancellationToken.None);

        AddParticipantResponse? participantBResult = await participantBResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationBId = participantBResult!.ParticipationId;

        await ClearAllRecordCategoriesAsync(dbContext);

        try
        {
            // Give both athletes valid totals
            await RecordAttempt(client, participationAId, Discipline.Bench, 1, 140.0m);
            await RecordAttempt(client, participationAId, Discipline.Deadlift, 1, 260.0m);
            await RecordAttempt(client, participationBId, Discipline.Bench, 1, 140.0m);
            await RecordAttempt(client, participationBId, Discipline.Deadlift, 1, 260.0m);

            // Athlete A squats 210kg
            await RecordAttempt(client, participationAId, Discipline.Squat, 1, 210.0m);

            // Act — Athlete B squats 220kg (heavier)
            await RecordAttempt(client, participationBId, Discipline.Squat, 1, 220.0m);
            await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

            // Assert — the current record should belong to Athlete B at 220kg
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
        finally
        {
            await CleanupEndpointTestParticipationsAsync(dbContext, participationAId, participationBId);
            await ClearAllRecordCategoriesAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenSecondLifterDoesNotBeatExistingRecord_NoNewRecord()
    {
        // Arrange
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await CleanupStaleTestDataFor83KgAsync(dbContext);

        DateOnly masters4DateOfBirth = new(1950, 1, 1);

        CreateAthleteCommand athleteACommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .Build();

        HttpResponseMessage athleteAResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteACommand,
            CancellationToken.None);

        athleteAResponse.EnsureSuccessStatusCode();

        string athleteASlug = Slug.Create($"{athleteACommand.FirstName} {athleteACommand.LastName}");

        CreateAthleteCommand athleteBCommand = new CreateAthleteCommandBuilder()
            .WithDateOfBirth(masters4DateOfBirth)
            .Build();

        HttpResponseMessage athleteBResponse = await client.PostAsJsonAsync(
            "/athletes",
            athleteBCommand,
            CancellationToken.None);

        athleteBResponse.EnsureSuccessStatusCode();

        string athleteBSlug = Slug.Create($"{athleteBCommand.FirstName} {athleteBCommand.LastName}");

        AddParticipantCommand participantACommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteASlug)
            .Build();

        HttpResponseMessage participantAResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantACommand,
            CancellationToken.None);

        AddParticipantResponse? participantAResult = await participantAResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationAId = participantAResult!.ParticipationId;

        AddParticipantCommand participantBCommand = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteBSlug)
            .Build();

        HttpResponseMessage participantBResponse = await client.PostAsJsonAsync(
            $"/meets/{SeedMeetId}/participants",
            participantBCommand,
            CancellationToken.None);

        AddParticipantResponse? participantBResult = await participantBResponse.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        int participationBId = participantBResult!.ParticipationId;

        await ClearAllRecordCategoriesAsync(dbContext);

        try
        {
            // Give both athletes valid totals
            await RecordAttempt(client, participationAId, Discipline.Bench, 1, 140.0m);
            await RecordAttempt(client, participationAId, Discipline.Deadlift, 1, 260.0m);
            await RecordAttempt(client, participationBId, Discipline.Bench, 1, 140.0m);
            await RecordAttempt(client, participationBId, Discipline.Deadlift, 1, 260.0m);

            // Athlete A squats 210kg — establishes the record
            await RecordAttempt(client, participationAId, Discipline.Squat, 1, 210.0m);

            // Act — Athlete B squats 205kg (less than A's 210kg)
            await RecordAttempt(client, participationBId, Discipline.Squat, 1, 205.0m);
            await fixture.WaitForRecordComputationAsync(TestContext.Current.CancellationToken);

            // Assert — current record should still belong to Athlete A at 210kg
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
        finally
        {
            await CleanupEndpointTestParticipationsAsync(dbContext, participationAId, participationBId);
            await ClearAllRecordCategoriesAsync(dbContext);
        }
    }

    [Fact]
    public async Task WhenAttemptMarkedNoGood_AllRecordsRevoked_SlotRebuilt()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int athleteId = 300;
        const int participationId = 300;
        const int squatAttemptId = 300;
        const int benchAttemptId = 301;
        const int deadliftAttemptId = 302;
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string seedSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {participationId};
            DELETE FROM Athletes WHERE AthleteId = {athleteId};

            DELETE FROM Records
            WHERE RecordCategoryId IN (1, 2, 3, 4, 5, 6) AND IsRaw = 1
            AND WeightCategoryId = {weightCategoryId};

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteId}, 'RecTest', 'One', '1950-01-01', 'm', 1, 'rectest-one');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({participationId}, {athleteId}, {TestSeedConstants.Meet.Id}, 90.0, {weightCategoryId}, {TestSeedConstants.AgeCategory.Masters4Id}, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 90.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({squatAttemptId}, {participationId}, 1, 1, 200.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({benchAttemptId}, {participationId}, 2, 1, 130.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({deadliftAttemptId}, {participationId}, 3, 1, 250.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedSql, TestContext.Current.CancellationToken);

        try
        {
            await service.ComputeRecordsAsync(squatAttemptId, CancellationToken.None);

            List<RecordEntity> recordsBefore = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == squatAttemptId || r.AttemptId == benchAttemptId || r.AttemptId == deadliftAttemptId)
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
            List<int> attemptIds = [squatAttemptId, benchAttemptId, deadliftAttemptId];

            List<RecordEntity> remainingRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId != null)
                .Where(r => attemptIds.Contains(r.AttemptId!.Value))
                .ToListAsync(CancellationToken.None);

            // Squat, Bench, Deadlift, and Total records should be revoked (no valid total).
            // BenchSingle and DeadliftSingle survive because single-lift records don't
            // require a valid total — 2 categories × 5 age categories = 10 records.
            remainingRecords.Count.ShouldBe(10);
            remainingRecords.ShouldAllBe(r =>
                r.RecordCategoryId == RecordCategory.BenchSingle ||
                r.RecordCategoryId == RecordCategory.DeadliftSingle);
        }
        finally
        {
            string cleanupSql =
                $"""
                DELETE FROM Records WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
                DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
                DELETE FROM Participations WHERE ParticipationId = {participationId};
                DELETE FROM Athletes WHERE AthleteId = {athleteId};
                """;

            await dbContext.Database.ExecuteSqlRawAsync(cleanupSql, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task WhenNoGoodOverturned_RecordsReEvaluated()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        SeedRecordAthlete athlete = await new RecordTestAthleteBuilder(dbContext, 310)
            .WithSquat(200m).WithBench(130m).WithDeadlift(200m)
            .BuildAsync(TestContext.Current.CancellationToken);

        await SeedRecordAthlete.ClearSlotAsync(
            dbContext,
            athlete.WeightCategoryId,
            TestContext.Current.CancellationToken);

        // Start with deadlift as a bad attempt — no valid total yet
        await dbContext.Set<Attempt>()
            .Where(a => a.AttemptId == athlete.DeadliftAttemptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(a => a.Good, false),
                TestContext.Current.CancellationToken);

        await dbContext.Set<Participation>()
            .Where(p => p.ParticipationId == athlete.ParticipationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Deadlift, 0m)
                      .SetProperty(p => p.Total, 0m),
                TestContext.Current.CancellationToken);

        dbContext.ChangeTracker.Clear();

        try
        {
            await service.ComputeRecordsAsync(athlete.DeadliftAttemptId, CancellationToken.None);

            List<int> attemptIds =
                [athlete.SquatAttemptId, athlete.BenchAttemptId, athlete.DeadliftAttemptId];

            List<RecordEntity> recordsBeforeOverturn = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId != null)
                .Where(r => attemptIds.Contains(r.AttemptId!.Value))
                .Where(r => r.IsCurrent)
                .ToListAsync(CancellationToken.None);

            recordsBeforeOverturn.ShouldBeEmpty();

            await dbContext.Set<Attempt>()
                .Where(a => a.AttemptId == athlete.DeadliftAttemptId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Good, true)
                          .SetProperty(a => a.Weight, 200m),
                    CancellationToken.None);

            await dbContext.Set<Participation>()
                .Where(p => p.ParticipationId == athlete.ParticipationId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.Deadlift, 200m)
                          .SetProperty(p => p.Total, 530m),
                    CancellationToken.None);

            dbContext.ChangeTracker.Clear();

            // Act
            await service.ComputeRecordsAsync(athlete.DeadliftAttemptId, CancellationToken.None);

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
        finally
        {
            await athlete.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task WhenAttemptWeightReduced_RecordRevoked_PreviousHolderRestored()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        SeedRecordAthlete athleteA = await new RecordTestAthleteBuilder(dbContext, 320)
            .WithSquat(200m).WithBench(130m).WithDeadlift(250m)
            .BuildAsync(TestContext.Current.CancellationToken);

        await SeedRecordAthlete.ClearSlotAsync(
            dbContext,
            athleteA.WeightCategoryId,
            TestContext.Current.CancellationToken);

        SeedRecordAthlete? athleteB = null;

        try
        {
            await service.ComputeRecordsAsync(athleteA.SquatAttemptId, CancellationToken.None);

            List<RecordEntity> recordsAfterA = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteA.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            recordsAfterA.ShouldNotBeEmpty();

            athleteB = await new RecordTestAthleteBuilder(dbContext, 330)
                .WithSquat(210m).WithBench(130m).WithDeadlift(250m)
                .BuildAsync(TestContext.Current.CancellationToken);

            dbContext.ChangeTracker.Clear();

            await service.ComputeRecordsAsync(athleteB.SquatAttemptId, CancellationToken.None);

            List<RecordEntity> recordsAfterB = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteB.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            recordsAfterB.ShouldNotBeEmpty();

            // Reduce B's squat below A's weight — total stays valid (all disciplines still good)
            await dbContext.Set<Attempt>()
                .Where(a => a.AttemptId == athleteB.SquatAttemptId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Weight, 190m),
                    CancellationToken.None);

            await dbContext.Set<Participation>()
                .Where(p => p.ParticipationId == athleteB.ParticipationId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.Squat, 190m)
                          .SetProperty(p => p.Total, 570m),
                    CancellationToken.None);

            dbContext.ChangeTracker.Clear();

            // Act
            await service.ComputeRecordsAsync(athleteB.SquatAttemptId, CancellationToken.None);

            // Assert
            List<RecordEntity> bCurrentSquatRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteB.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            bCurrentSquatRecords.ShouldBeEmpty();

            List<RecordEntity> aRestoredRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteA.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            aRestoredRecords.ShouldNotBeEmpty();
            aRestoredRecords.ShouldAllBe(r => r.Weight == 200m);
        }
        finally
        {
            await athleteA.DeleteAsync(dbContext, TestContext.Current.CancellationToken);

            if (athleteB is not null)
            {
                await athleteB.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task WhenAttemptWeightCorrected_SameAttemptId_RecordWeightUpdated()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        SeedRecordAthlete athlete = await new RecordTestAthleteBuilder(dbContext, 340)
            .WithSquat(200m).WithBench(130m).WithDeadlift(250m)
            .BuildAsync(TestContext.Current.CancellationToken);

        await SeedRecordAthlete.ClearSlotAsync(
            dbContext,
            athlete.WeightCategoryId,
            TestContext.Current.CancellationToken);

        try
        {
            await service.ComputeRecordsAsync(athlete.SquatAttemptId, CancellationToken.None);

            List<RecordEntity> initialRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athlete.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            initialRecords.ShouldNotBeEmpty();
            initialRecords.ShouldAllBe(r => r.Weight == 200m);

            await dbContext.Set<Attempt>()
                .Where(a => a.AttemptId == athlete.SquatAttemptId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Weight, 210m),
                    CancellationToken.None);

            await dbContext.Set<Participation>()
                .Where(p => p.ParticipationId == athlete.ParticipationId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.Squat, 210m)
                          .SetProperty(p => p.Total, 590m),
                    CancellationToken.None);

            dbContext.ChangeTracker.Clear();

            // Act
            await service.ComputeRecordsAsync(athlete.SquatAttemptId, CancellationToken.None);

            // Assert
            List<RecordEntity> oldWeightRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athlete.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .Where(r => r.Weight == 200m)
                .ToListAsync(CancellationToken.None);

            oldWeightRecords.ShouldBeEmpty();

            List<RecordEntity> updatedRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athlete.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .Where(r => r.Weight == 210m)
                .ToListAsync(CancellationToken.None);

            updatedRecords.ShouldNotBeEmpty();
        }
        finally
        {
            await athlete.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task WhenTotalReduced_TotalRecordRevoked_SlotRebuilt()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        SeedRecordAthlete athleteA = await new RecordTestAthleteBuilder(dbContext, 350)
            .WithBench(150m)
            .BuildAsync(TestContext.Current.CancellationToken);

        SeedRecordAthlete? athleteB = null;

        try
        {
            await service.ComputeRecordsAsync(athleteA.DeadliftAttemptId, CancellationToken.None);

            List<RecordEntity> aTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteA.DeadliftAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            aTotalRecords.ShouldNotBeEmpty();

            athleteB = await new RecordTestAthleteBuilder(dbContext, 360)
                .WithSquat(250m)
                .WithBench(150m)
                .WithDeadlift(300m)
                .BuildAsync(TestContext.Current.CancellationToken);

            dbContext.ChangeTracker.Clear();

            await service.ComputeRecordsAsync(athleteB.DeadliftAttemptId, CancellationToken.None);

            List<RecordEntity> bTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteB.DeadliftAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            bTotalRecords.ShouldNotBeEmpty();

            // Reduce B's deadlift so total drops below A's — all disciplines stay good
            await dbContext.Set<Attempt>()
                .Where(a => a.AttemptId == athleteB.DeadliftAttemptId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Weight, 100m),
                    CancellationToken.None);

            await dbContext.Set<Participation>()
                .Where(p => p.ParticipationId == athleteB.ParticipationId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.Deadlift, 100m)
                          .SetProperty(p => p.Total, 500m),
                    CancellationToken.None);

            dbContext.ChangeTracker.Clear();

            // Act
            await service.ComputeRecordsAsync(athleteB.DeadliftAttemptId, CancellationToken.None);

            // Assert
            List<RecordEntity> bFinalTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteB.DeadliftAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            bFinalTotalRecords.ShouldBeEmpty();

            List<RecordEntity> aRestoredTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteA.DeadliftAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            aRestoredTotalRecords.ShouldNotBeEmpty();
        }
        finally
        {
            await athleteA.DeleteAsync(dbContext, TestContext.Current.CancellationToken);

            if (athleteB is not null)
            {
                await athleteB.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task WhenThreeAthletesProgressivelySetTotalRecord_AllPreservedInHistory()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        SeedRecordAthlete athleteA = await new RecordTestAthleteBuilder(dbContext, 400)
            .WithSquat(210m)
            .WithBench(140m)
            .WithDeadlift(260m)
            .BuildAsync(TestContext.Current.CancellationToken);

        SeedRecordAthlete athleteB = await new RecordTestAthleteBuilder(dbContext, 403)
            .WithSquat(210m)
            .WithBench(140m)
            .WithDeadlift(270m)
            .BuildAsync(TestContext.Current.CancellationToken);

        SeedRecordAthlete athleteC = await new RecordTestAthleteBuilder(dbContext, 406)
            .WithSquat(210m)
            .WithBench(140m)
            .WithDeadlift(280m)
            .BuildAsync(TestContext.Current.CancellationToken);

        try
        {
            // Compute records for A (total=610), then B (total=620), then C (total=630)
            await service.ComputeRecordsAsync(athleteA.DeadliftAttemptId, CancellationToken.None);
            await service.ComputeRecordsAsync(athleteB.DeadliftAttemptId, CancellationToken.None);

            // Act
            await service.ComputeRecordsAsync(athleteC.DeadliftAttemptId, CancellationToken.None);

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
        finally
        {
            await athleteA.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
            await athleteB.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
            await athleteC.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task WhenParticipantRemoved_RecordsRevoked_SlotsRebuilt()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        SeedRecordAthlete athleteA = await new RecordTestAthleteBuilder(dbContext, 410)
            .WithSquat(211m)
            .WithBench(140m)
            .WithDeadlift(260m)
            .BuildAsync(TestContext.Current.CancellationToken);

        SeedRecordAthlete? athleteB = null;

        try
        {
            await service.ComputeRecordsAsync(athleteA.SquatAttemptId, CancellationToken.None);

            List<RecordEntity> recordsAfterA = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteA.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            recordsAfterA.ShouldNotBeEmpty();

            // Seed athlete B with squat=220 (beats A's 211)
            athleteB = await new RecordTestAthleteBuilder(dbContext, 420)
                .WithSquat(220m)
                .WithBench(140m)
                .WithDeadlift(260m)
                .BuildAsync(TestContext.Current.CancellationToken);

            dbContext.ChangeTracker.Clear();

            await service.ComputeRecordsAsync(athleteB.SquatAttemptId, CancellationToken.None);

            List<RecordEntity> bRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == athleteB.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            bRecords.ShouldNotBeEmpty();

            // Act — simulate participant B removal: collect affected slots, delete records + participation, rebuild
            List<int> bAttemptIds =
            [
                athleteB.SquatAttemptId,
                athleteB.BenchAttemptId,
                athleteB.DeadliftAttemptId,
            ];

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
                DELETE FROM Records WHERE AttemptId IN ({athleteB.SquatAttemptId}, {athleteB.BenchAttemptId}, {athleteB.DeadliftAttemptId});
                DELETE FROM Attempts WHERE AttemptId IN ({athleteB.SquatAttemptId}, {athleteB.BenchAttemptId}, {athleteB.DeadliftAttemptId});
                DELETE FROM Participations WHERE ParticipationId = {athleteB.ParticipationId};
                """;

            await dbContext.Database.ExecuteSqlRawAsync(deleteBSql, TestContext.Current.CancellationToken);

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
                .Where(r => r.AttemptId == athleteA.SquatAttemptId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            aRestoredRecords.ShouldNotBeEmpty();
            aRestoredRecords.ShouldAllBe(r => r.Weight == 211m);
        }
        finally
        {
            await athleteA.DeleteAsync(dbContext, TestContext.Current.CancellationToken);

            if (athleteB is not null)
            {
                await athleteB.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task WhenBenchOnlyMeet_AttemptNoGood_RecordRevoked()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int benchMeetId = 50;
        const int benchMeetTypeId = 2;
        const int athleteId = 430;
        const int participationId = 430;
        const int benchAttemptId = 430;
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string seedSql =
            $"""
            DELETE FROM Records WHERE WeightCategoryId = {weightCategoryId} AND IsRaw = 1 AND EraId = 2;
            DELETE FROM Records WHERE AttemptId = {benchAttemptId};
            DELETE FROM Attempts WHERE AttemptId = {benchAttemptId};
            DELETE FROM Participations WHERE ParticipationId = {participationId};
            DELETE FROM Athletes WHERE AthleteId = {athleteId};
            DELETE FROM Participations WHERE MeetId = {benchMeetId};
            DELETE FROM Meets WHERE MeetId = {benchMeetId};

            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({benchMeetId}, 'Bench Press Meet 2025', 'bench-press-meet-2025', '2025-06-01', '2025-06-01', 1, 1, 1, 1, {benchMeetTypeId}, 0, 1, 0, 1, 0, 1, 1);
            SET IDENTITY_INSERT Meets OFF;

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteId}, 'RecTest', 'Eight', '1950-01-01', 'm', 1, 'rectest-eight');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({participationId}, {athleteId}, {benchMeetId}, 90.0, {weightCategoryId}, {TestSeedConstants.AgeCategory.Masters4Id}, 1, 0, 0.0, 150.0, 0.0, 150.0, 100.0, 50.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({benchAttemptId}, {participationId}, 2, 1, 150.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedSql, TestContext.Current.CancellationToken);

        try
        {
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
        finally
        {
            string cleanupSql =
                $"""
                DELETE FROM Records WHERE AttemptId = {benchAttemptId};
                DELETE FROM Attempts WHERE AttemptId = {benchAttemptId};
                DELETE FROM Participations WHERE ParticipationId = {participationId};
                DELETE FROM Athletes WHERE AthleteId = {athleteId};
                DELETE FROM Participations WHERE MeetId = {benchMeetId};
                DELETE FROM Meets WHERE MeetId = {benchMeetId};
                """;

            await dbContext.Database.ExecuteSqlRawAsync(cleanupSql, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task WhenBackfillRunsAfterComputation_NoChanges()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        SeedRecordAthlete athleteA = await new RecordTestAthleteBuilder(dbContext, 440)
            .BuildAsync(TestContext.Current.CancellationToken);

        SeedRecordAthlete athleteB = await new RecordTestAthleteBuilder(dbContext, 450)
            .WithSquat(220m)
            .WithBench(140m)
            .WithDeadlift(260m)
            .BuildAsync(TestContext.Current.CancellationToken);

        try
        {
            // Compute records for A's attempts, then B's
            await service.ComputeRecordsAsync(athleteA.SquatAttemptId, CancellationToken.None);
            await service.ComputeRecordsAsync(athleteA.BenchAttemptId, CancellationToken.None);
            await service.ComputeRecordsAsync(athleteA.DeadliftAttemptId, CancellationToken.None);
            await service.ComputeRecordsAsync(athleteB.SquatAttemptId, CancellationToken.None);
            await service.ComputeRecordsAsync(athleteB.BenchAttemptId, CancellationToken.None);
            await service.ComputeRecordsAsync(athleteB.DeadliftAttemptId, CancellationToken.None);

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
            await using AsyncServiceScope assertScope = fixture.Factory.Services.CreateAsyncScope();
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
        finally
        {
            // Backfill may have modified global records; do a full restore
            string cleanupSql =
                $"""
                DELETE FROM Records;
                DELETE FROM Attempts WHERE AttemptId IN ({athleteA.SquatAttemptId}, {athleteA.BenchAttemptId}, {athleteA.DeadliftAttemptId}, {athleteB.SquatAttemptId}, {athleteB.BenchAttemptId}, {athleteB.DeadliftAttemptId});
                DELETE FROM Participations WHERE ParticipationId IN ({athleteA.ParticipationId}, {athleteB.ParticipationId});
                DELETE FROM Athletes WHERE AthleteId IN ({athleteA.AthleteId}, {athleteB.AthleteId});
                """;

            await dbContext.Database.ExecuteSqlRawAsync(cleanupSql, TestContext.Current.CancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                BaseSeedSql.SeedBaseRecords(),
                TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task WhenNonIcelandicAthleteHasBetterLift_SlotRebuildIgnoresNonIcelander()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int weightCategoryId = TestSeedConstants.WeightCategory.Id105Kg;
        const int norwayCountryId = 2;

        await SeedRecordAthlete.ClearSlotAsync(dbContext, weightCategoryId, TestContext.Current.CancellationToken);

        SeedRecordAthlete norwegianAthlete = await new RecordTestAthleteBuilder(dbContext, 500)
            .WithCountryId(norwayCountryId)
            .WithWeightCategoryId(weightCategoryId)
            .WithSquat(250m)
            .WithBench(150m)
            .WithDeadlift(300m)
            .BuildAsync(TestContext.Current.CancellationToken);

        SeedRecordAthlete icelandicAthlete = await new RecordTestAthleteBuilder(dbContext, 510)
            .WithWeightCategoryId(weightCategoryId)
            .BuildAsync(TestContext.Current.CancellationToken);

        try
        {
            // Act — compute records triggered by the Icelandic athlete's squat
            await service.ComputeRecordsAsync(icelandicAthlete.SquatAttemptId, CancellationToken.None);

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
            currentSquatRecords.ShouldAllBe(r => r.AttemptId == icelandicAthlete.SquatAttemptId);
            currentSquatRecords.ShouldAllBe(r => r.Weight == 200m);

            List<RecordEntity> norwegianRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == norwegianAthlete.SquatAttemptId)
                .ToListAsync(CancellationToken.None);

            norwegianRecords.ShouldBeEmpty();
        }
        finally
        {
            await norwegianAthlete.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
            await icelandicAthlete.DeleteAsync(dbContext, TestContext.Current.CancellationToken);
        }
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

    private static async Task CleanupEndpointTestParticipationsAsync(
        ResultsDbContext dbContext,
        params int[] participationIds)
    {
        string ids = string.Join(", ", participationIds);
        string sql =
            $"""
            DELETE FROM Records WHERE AttemptId IN (SELECT AttemptId FROM Attempts WHERE ParticipationId IN ({ids}));
            DELETE FROM Attempts WHERE ParticipationId IN ({ids});
            DELETE FROM Participations WHERE ParticipationId IN ({ids});
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// Removes non-seed participations (and their attempts/records) from the 83kg slot
    /// in the seed meet. This prevents leftover data from prior endpoint-based tests
    /// from interfering with the FullRebuildSlotsAsync computation.
    /// </summary>
    private static async Task CleanupStaleTestDataFor83KgAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            DELETE FROM Records WHERE AttemptId IN (
                SELECT a.AttemptId FROM Attempts a
                INNER JOIN Participations p ON a.ParticipationId = p.ParticipationId
                WHERE p.WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg}
                AND p.MeetId = {TestSeedConstants.Meet.Id}
                AND p.ParticipationId NOT IN (1, 2, 3));
            DELETE FROM Attempts WHERE ParticipationId IN (
                SELECT ParticipationId FROM Participations
                WHERE WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg}
                AND MeetId = {TestSeedConstants.Meet.Id}
                AND ParticipationId NOT IN (1, 2, 3));
            DELETE FROM Participations
            WHERE WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg}
            AND MeetId = {TestSeedConstants.Meet.Id}
            AND ParticipationId NOT IN (1, 2, 3);
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task ClearAllRecordCategoriesAsync(ResultsDbContext dbContext)
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
            AND RecordCategoryId IN (1, 2, 3, 4, 5, 6)
            AND IsRaw = 1
            AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task CleanupDirectSeedTestDataAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            DELETE FROM Records WHERE AttemptId = {RecordTestAttemptId};
            DELETE FROM Attempts WHERE AttemptId = {RecordTestAttemptId};
            DELETE FROM Participations WHERE ParticipationId = {RecordTestParticipationId};

            -- Restore athlete DoB changed by SeedRecordComputationTestDataAsync
            UPDATE Athletes SET DateOfBirth = '{TestSeedConstants.Athlete.DateOfBirth:yyyy-MM-dd}' WHERE AthleteId = {TestSeedConstants.Athlete.Id};

            -- Restore the open raw squat 83kg record that was deleted by SeedRecordComputationTestDataAsync
            IF NOT EXISTS (
                SELECT 1 FROM Records
                WHERE EraId = {TestSeedConstants.Era.CurrentId}
                AND AgeCategoryId = {TestSeedConstants.AgeCategory.OpenId}
                AND WeightCategoryId = {TestSeedConstants.WeightCategory.Id83Kg}
                AND RecordCategoryId = 1
                AND IsRaw = 1)
            BEGIN
                INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
                VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, 195.0, '2025-03-15', 0, 1, 1, 1, 'seed');
            END
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

            -- Set athlete DoB to Masters4 range so biological age cascades correctly
            UPDATE Athletes SET DateOfBirth = '1950-01-01' WHERE AthleteId = {TestSeedConstants.Athlete.Id};

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