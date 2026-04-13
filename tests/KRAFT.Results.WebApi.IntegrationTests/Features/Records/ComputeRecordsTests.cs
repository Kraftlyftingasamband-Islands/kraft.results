using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;
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
    public async Task WhenAllDisciplinesRecorded_BenchRecordIsAlsoCreated()
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

        // Record squat, bench, then deadlift (deadlift triggers the last event)
        await RecordAttempt(client, participationId, Discipline.Squat, 1, 150.0m);
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 100.0m);

        // Act — deadlift completes the total, enabling all records
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 180.0m);

        // Assert — bench record should exist (not just the triggering deadlift)
        List<RecordEntity> benchRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Bench)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 100.0m)
            .ToListAsync(CancellationToken.None);

        benchRecords.ShouldNotBeEmpty();

        // Clean up — restore records to seed state
        await ClearAllRecordCategoriesAsync(dbContext);
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_SquatRecordIsAlsoCreated()
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

        // Record squat first (no valid total yet), then bench, then deadlift
        await RecordAttempt(client, participationId, Discipline.Squat, 1, 150.0m);
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 100.0m);

        // Act — deadlift completes the total, enabling all records
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 180.0m);

        // Assert — squat record should exist even though it was recorded before total was valid
        List<RecordEntity> squatRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 150.0m)
            .ToListAsync(CancellationToken.None);

        squatRecords.ShouldNotBeEmpty();

        // Clean up — restore records to seed state
        await ClearAllRecordCategoriesAsync(dbContext);
    }

    [Fact]
    public async Task WhenAllDisciplinesRecorded_TotalRecordIsCreated()
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

        await RecordAttempt(client, participationId, Discipline.Squat, 1, 150.0m);
        await RecordAttempt(client, participationId, Discipline.Bench, 1, 100.0m);

        // Act — deadlift completes the total
        await RecordAttempt(client, participationId, Discipline.Deadlift, 1, 180.0m);

        // Assert — total record should exist with weight = 150 + 100 + 180 = 430
        List<RecordEntity> totalRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 430.0m)
            .ToListAsync(CancellationToken.None);

        totalRecords.ShouldNotBeEmpty();

        // Clean up — restore records to seed state
        await ClearAllRecordCategoriesAsync(dbContext);
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

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearAllRecordCategoriesAsync(dbContext);

        // Give both athletes valid totals
        await RecordAttempt(client, participationAId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationAId, Discipline.Deadlift, 1, 250.0m);
        await RecordAttempt(client, participationBId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationBId, Discipline.Deadlift, 1, 250.0m);

        // Athlete A squats 200kg
        await RecordAttempt(client, participationAId, Discipline.Squat, 1, 200.0m);

        // Act — Athlete B squats 210kg (heavier)
        await RecordAttempt(client, participationBId, Discipline.Squat, 1, 210.0m);

        // Assert — the current record should belong to Athlete B at 210kg
        List<RecordEntity> currentRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.Weight == 210.0m)
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
        HttpClient client = fixture.CreateAuthorizedHttpClientWithRecordComputation();

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

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        await ClearAllRecordCategoriesAsync(dbContext);

        // Give both athletes valid totals
        await RecordAttempt(client, participationAId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationAId, Discipline.Deadlift, 1, 250.0m);
        await RecordAttempt(client, participationBId, Discipline.Bench, 1, 130.0m);
        await RecordAttempt(client, participationBId, Discipline.Deadlift, 1, 250.0m);

        // Athlete A squats 200kg — establishes the record
        await RecordAttempt(client, participationAId, Discipline.Squat, 1, 200.0m);

        // Act — Athlete B squats 190kg (less than A's 200kg)
        await RecordAttempt(client, participationBId, Discipline.Squat, 1, 190.0m);

        // Assert — current record should still belong to Athlete A at 200kg
        List<RecordEntity> currentRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.IsCurrent)
            .Where(r => r.IsRaw)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Include(r => r.AgeCategory)
            .OrderBy(r => r.AgeCategoryId)
            .ToListAsync(CancellationToken.None);

        currentRecords.ShouldAllBe(r => r.Weight == 200.0m);

        List<string> cascadeSlugs = currentRecords
            .Select(r => r.AgeCategory.Slug!)
            .ToList();

        cascadeSlugs.Count.ShouldBe(5);
        cascadeSlugs.ShouldContain("masters4");
        cascadeSlugs.ShouldContain("masters3");
        cascadeSlugs.ShouldContain("masters2");
        cascadeSlugs.ShouldContain("masters1");
        cascadeSlugs.ShouldContain("open");

        // No records should exist for Athlete B's attempt weight
        List<RecordEntity> athleteBRecords = await dbContext.Set<RecordEntity>()
            .Where(r => r.Weight == 190.0m)
            .Where(r => r.RecordCategoryId == RecordCategory.Squat)
            .Where(r => r.WeightCategoryId == TestSeedConstants.WeightCategory.Id83Kg)
            .Where(r => r.IsRaw)
            .ToListAsync(CancellationToken.None);

        athleteBRecords.ShouldBeEmpty();
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
            WHERE RecordCategoryId IN (1, 2, 3, 4) AND IsRaw = 1
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

            remainingRecords.ShouldBeEmpty();
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

        const int athleteId = 310;
        const int participationId = 310;
        const int squatAttemptId = 310;
        const int benchAttemptId = 311;
        const int deadliftAttemptId = 312;
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string seedSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {participationId};
            DELETE FROM Athletes WHERE AthleteId = {athleteId};

            DELETE FROM Records
            WHERE RecordCategoryId IN (1, 2, 3, 4) AND IsRaw = 1
            AND WeightCategoryId = {weightCategoryId};

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteId}, 'RecTest', 'Two', '1950-01-01', 'm', 1, 'rectest-two');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({participationId}, {athleteId}, {TestSeedConstants.Meet.Id}, 90.0, {weightCategoryId}, {TestSeedConstants.AgeCategory.Masters4Id}, 1, 0, 200.0, 130.0, 0.0, 0.0, 0.0, 0.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({squatAttemptId}, {participationId}, 1, 1, 200.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({benchAttemptId}, {participationId}, 2, 1, 130.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({deadliftAttemptId}, {participationId}, 3, 1, 200.0, 0, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedSql, TestContext.Current.CancellationToken);

        try
        {
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
    public async Task WhenAttemptMarkedNoGood_RecordRevoked_PreviousHolderRestored()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int athleteAId = 320;
        const int participationAId = 320;
        const int squatAttemptAId = 320;
        const int benchAttemptAId = 321;
        const int deadliftAttemptAId = 322;

        const int athleteBId = 330;
        const int participationBId = 330;
        const int squatAttemptBId = 330;
        const int benchAttemptBId = 331;
        const int deadliftAttemptBId = 332;
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string seedSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
            DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
            DELETE FROM Participations WHERE ParticipationId IN ({participationAId}, {participationBId});
            DELETE FROM Athletes WHERE AthleteId IN ({athleteAId}, {athleteBId});

            DELETE FROM Records
            WHERE RecordCategoryId IN (1, 2, 3, 4) AND IsRaw = 1
            AND WeightCategoryId = {weightCategoryId};

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteAId}, 'RecTest', 'ThreeA', '1950-01-01', 'm', 1, 'rectest-threea');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteBId}, 'RecTest', 'ThreeB', '1950-01-01', 'm', 1, 'rectest-threeb');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({participationAId}, {athleteAId}, {TestSeedConstants.Meet.Id}, 90.0, {weightCategoryId}, {TestSeedConstants.AgeCategory.Masters4Id}, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 90.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({squatAttemptAId}, {participationAId}, 1, 1, 200.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({benchAttemptAId}, {participationAId}, 2, 1, 130.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({deadliftAttemptAId}, {participationAId}, 3, 1, 250.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedSql, TestContext.Current.CancellationToken);

        try
        {
            await service.ComputeRecordsAsync(squatAttemptAId, CancellationToken.None);

            List<RecordEntity> recordsAfterA = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == squatAttemptAId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            recordsAfterA.ShouldNotBeEmpty();

            string seedBSql =
                $"""
                SET IDENTITY_INSERT Participations ON;
                INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
                VALUES ({participationBId}, {athleteBId}, {TestSeedConstants.Meet.Id}, 92.0, {weightCategoryId}, {TestSeedConstants.AgeCategory.Masters4Id}, 2, 0, 210.0, 130.0, 250.0, 590.0, 410.0, 92.0, 51);
                SET IDENTITY_INSERT Participations OFF;

                SET IDENTITY_INSERT Attempts ON;
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({squatAttemptBId}, {participationBId}, 1, 1, 210.0, 1, 'test', 'test');
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({benchAttemptBId}, {participationBId}, 2, 1, 130.0, 1, 'test', 'test');
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({deadliftAttemptBId}, {participationBId}, 3, 1, 250.0, 1, 'test', 'test');
                SET IDENTITY_INSERT Attempts OFF;
                """;

            await dbContext.Database.ExecuteSqlRawAsync(seedBSql, TestContext.Current.CancellationToken);

            dbContext.ChangeTracker.Clear();

            await service.ComputeRecordsAsync(squatAttemptBId, CancellationToken.None);

            List<RecordEntity> recordsAfterB = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == squatAttemptBId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            recordsAfterB.ShouldNotBeEmpty();

            await dbContext.Set<Attempt>()
                .Where(a => a.AttemptId == squatAttemptBId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Good, false),
                    CancellationToken.None);

            await dbContext.Set<Participation>()
                .Where(p => p.ParticipationId == participationBId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.Squat, 0m)
                          .SetProperty(p => p.Total, 0m),
                    CancellationToken.None);

            dbContext.ChangeTracker.Clear();

            // Act
            await service.ComputeRecordsAsync(squatAttemptBId, CancellationToken.None);

            // Assert
            List<RecordEntity> bCurrentSquatRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == squatAttemptBId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            bCurrentSquatRecords.ShouldBeEmpty();

            List<RecordEntity> aRestoredRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == squatAttemptAId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Squat)
                .ToListAsync(CancellationToken.None);

            aRestoredRecords.ShouldNotBeEmpty();
            aRestoredRecords.ShouldAllBe(r => r.Weight == 200m);
        }
        finally
        {
            string cleanupSql =
                $"""
                DELETE FROM Records WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
                DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
                DELETE FROM Participations WHERE ParticipationId IN ({participationAId}, {participationBId});
                DELETE FROM Athletes WHERE AthleteId IN ({athleteAId}, {athleteBId});
                """;

            await dbContext.Database.ExecuteSqlRawAsync(cleanupSql, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task WhenAttemptWeightCorrected_SameAttemptId_RecordWeightUpdated()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int athleteId = 340;
        const int participationId = 340;
        const int squatAttemptId = 340;
        const int benchAttemptId = 341;
        const int deadliftAttemptId = 342;
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string seedSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptId}, {benchAttemptId}, {deadliftAttemptId});
            DELETE FROM Participations WHERE ParticipationId = {participationId};
            DELETE FROM Athletes WHERE AthleteId = {athleteId};

            DELETE FROM Records
            WHERE RecordCategoryId IN (1, 2, 3, 4) AND IsRaw = 1
            AND WeightCategoryId = {weightCategoryId};

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteId}, 'RecTest', 'Four', '1950-01-01', 'm', 1, 'rectest-four');
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
    public async Task WhenDeadliftNoGood_TotalRecordRevoked_PreviousHolderRestored()
    {
        // Arrange
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        RecordComputationService service = scope.ServiceProvider.GetRequiredService<RecordComputationService>();

        const int athleteAId = 350;
        const int participationAId = 350;
        const int squatAttemptAId = 350;
        const int benchAttemptAId = 351;
        const int deadliftAttemptAId = 352;

        const int athleteBId = 360;
        const int participationBId = 360;
        const int squatAttemptBId = 360;
        const int benchAttemptBId = 361;
        const int deadliftAttemptBId = 362;
        const int weightCategoryId = TestSeedConstants.WeightCategory.Id93Kg;

        string seedSql =
            $"""
            DELETE FROM Records WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
            DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
            DELETE FROM Participations WHERE ParticipationId IN ({participationAId}, {participationBId});
            DELETE FROM Athletes WHERE AthleteId IN ({athleteAId}, {athleteBId});

            DELETE FROM Records
            WHERE RecordCategoryId IN (1, 2, 3, 4) AND IsRaw = 1
            AND WeightCategoryId = {weightCategoryId};

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteAId}, 'RecTest', 'FiveA', '1950-01-01', 'm', 1, 'rectest-fivea');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({athleteBId}, 'RecTest', 'FiveB', '1950-01-01', 'm', 1, 'rectest-fiveb');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({participationAId}, {athleteAId}, {TestSeedConstants.Meet.Id}, 90.0, {weightCategoryId}, {TestSeedConstants.AgeCategory.Masters4Id}, 1, 0, 200.0, 150.0, 250.0, 600.0, 400.0, 90.0, 50);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({squatAttemptAId}, {participationAId}, 1, 1, 200.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({benchAttemptAId}, {participationAId}, 2, 1, 150.0, 1, 'test', 'test');
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({deadliftAttemptAId}, {participationAId}, 3, 1, 250.0, 1, 'test', 'test');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedSql, TestContext.Current.CancellationToken);

        try
        {
            await service.ComputeRecordsAsync(deadliftAttemptAId, CancellationToken.None);

            List<RecordEntity> aTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == deadliftAttemptAId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            aTotalRecords.ShouldNotBeEmpty();

            string seedBSql =
                $"""
                SET IDENTITY_INSERT Participations ON;
                INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
                VALUES ({participationBId}, {athleteBId}, {TestSeedConstants.Meet.Id}, 92.0, {weightCategoryId}, {TestSeedConstants.AgeCategory.Masters4Id}, 2, 0, 250.0, 150.0, 300.0, 700.0, 450.0, 95.0, 51);
                SET IDENTITY_INSERT Participations OFF;

                SET IDENTITY_INSERT Attempts ON;
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({squatAttemptBId}, {participationBId}, 1, 1, 250.0, 1, 'test', 'test');
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({benchAttemptBId}, {participationBId}, 2, 1, 150.0, 1, 'test', 'test');
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({deadliftAttemptBId}, {participationBId}, 3, 1, 300.0, 1, 'test', 'test');
                SET IDENTITY_INSERT Attempts OFF;
                """;

            await dbContext.Database.ExecuteSqlRawAsync(seedBSql, TestContext.Current.CancellationToken);

            dbContext.ChangeTracker.Clear();

            await service.ComputeRecordsAsync(deadliftAttemptBId, CancellationToken.None);

            List<RecordEntity> bTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == deadliftAttemptBId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            bTotalRecords.ShouldNotBeEmpty();

            await dbContext.Set<Attempt>()
                .Where(a => a.AttemptId == deadliftAttemptBId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Good, false),
                    CancellationToken.None);

            await dbContext.Set<Participation>()
                .Where(p => p.ParticipationId == participationBId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.Deadlift, 0m)
                          .SetProperty(p => p.Total, 0m),
                    CancellationToken.None);

            dbContext.ChangeTracker.Clear();

            // Act
            await service.ComputeRecordsAsync(deadliftAttemptBId, CancellationToken.None);

            // Assert
            List<RecordEntity> bFinalTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == deadliftAttemptBId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            bFinalTotalRecords.ShouldBeEmpty();

            List<RecordEntity> aRestoredTotalRecords = await dbContext.Set<RecordEntity>()
                .Where(r => r.AttemptId == deadliftAttemptAId)
                .Where(r => r.IsCurrent)
                .Where(r => r.RecordCategoryId == RecordCategory.Total)
                .ToListAsync(CancellationToken.None);

            aRestoredTotalRecords.ShouldNotBeEmpty();
        }
        finally
        {
            string cleanupSql =
                $"""
                DELETE FROM Records WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
                DELETE FROM Attempts WHERE AttemptId IN ({squatAttemptAId}, {benchAttemptAId}, {deadliftAttemptAId}, {squatAttemptBId}, {benchAttemptBId}, {deadliftAttemptBId});
                DELETE FROM Participations WHERE ParticipationId IN ({participationAId}, {participationBId});
                DELETE FROM Athletes WHERE AthleteId IN ({athleteAId}, {athleteBId});
                """;

            await dbContext.Database.ExecuteSqlRawAsync(cleanupSql, TestContext.Current.CancellationToken);
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
            AND RecordCategoryId IN (1, 2, 3, 4)
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