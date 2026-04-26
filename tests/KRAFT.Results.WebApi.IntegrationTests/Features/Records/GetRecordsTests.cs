using System.Data.Common;
using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(GetRecordsTestsCollection))]
public sealed class GetRecordsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/records";

    // Weight constants — equipped meet 1 (earlier date, produces "lower" historical records)
    private const decimal Meet1SquatWeight = 190.0m;
    private const decimal Meet1BenchWeight = 100.0m;
    private const decimal Meet1DeadliftWeight = 200.0m;

    // Weight constants — equipped meet 2 (later date, produces current records)
    private const decimal EquippedSquatWeight = 200.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;

    // Weight constants — classic meet
    private const decimal ClassicSquatWeight = 195.0m;
    private const decimal ClassicBenchWeight = 130.0m;
    private const decimal ClassicDeadliftWeight = 250.0m;

    // Weight constants — junior meets (83kg weight category)
    private const decimal JuniorSquat83Weight = 180.0m;
    private const decimal JuniorBench83Weight = 90.0m;
    private const decimal JuniorDeadlift83Weight = 180.0m;

    // Weight constants — junior meets (74kg JuniorsOnly weight category)
    private const decimal JuniorSquat74Weight = 170.0m;
    private const decimal JuniorBench74Weight = 80.0m;
    private const decimal JuniorDeadlift74Weight = 160.0m;

    // Weight constants — SQL-only records
    private const decimal StandardSquatWeight = 220.0m;
    private const decimal TotalWilksWeight = 400.0m;
    private const decimal TotalIpfPointsWeight = 85.5m;
    private const decimal CorruptBenchHigherWeight = 150.0m;
    private const decimal CorruptBenchLowerWeight = 140.0m;
    private const decimal CorruptIsStandardBenchSingleWeight = 130.0m;
    private const decimal NoEraWcWeight = 230.0m;

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<int> _meetIds = [];
    private readonly List<int> _sqlRecordIds = [];
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        // Athletes
        string athleteASlug = await CreateAthleteAsync("RecA", "m", new DateOnly(1985, 7, 2));
        string athleteBSlug = await CreateAthleteAsync("RecB", "m", new DateOnly(2003, 1, 1));

        // Equipped meet 1 — earlier date, produces historical "lower" records
        int equippedMeet1Id = await CreateMeetAndGetIdAsync(
            new DateOnly(2024, 6, 1), isRaw: false);

        // Equipped meet 2 — later date, produces current records
        int equippedMeet2Id = await CreateMeetAndGetIdAsync(
            new DateOnly(2025, 3, 15), isRaw: false);

        // Classic meet — produces IsRaw=true (classic) records
        int classicMeetId = await CreateMeetAndGetIdAsync(
            new DateOnly(2025, 3, 15), isRaw: true);

        // Junior equipped meet for 83kg
        int juniorMeet83Id = await CreateMeetAndGetIdAsync(
            new DateOnly(2025, 3, 15), isRaw: false);

        // Junior equipped meet for 74kg JuniorsOnly
        int juniorMeet74Id = await CreateMeetAndGetIdAsync(
            new DateOnly(2025, 3, 15), isRaw: false);

        // --- Athlete A participations ---

        // Meet 1: athlete A at 83kg, lower squat record
        int p1Id = await AddParticipantAsync(equippedMeet1Id, athleteASlug, 80.5m);
        await RecordAttemptAsync(equippedMeet1Id, p1Id, Discipline.Squat, 1, Meet1SquatWeight);
        await RecordAttemptAsync(equippedMeet1Id, p1Id, Discipline.Bench, 1, Meet1BenchWeight);
        await RecordAttemptAsync(equippedMeet1Id, p1Id, Discipline.Deadlift, 1, Meet1DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Meet 2: athlete A at 83kg, higher squat record (supersedes meet 1)
        int p2Id = await AddParticipantAsync(equippedMeet2Id, athleteASlug, 80.5m);
        await RecordAttemptAsync(equippedMeet2Id, p2Id, Discipline.Squat, 1, EquippedSquatWeight);
        await RecordAttemptAsync(equippedMeet2Id, p2Id, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(equippedMeet2Id, p2Id, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Classic meet: athlete A at 83kg
        int p3Id = await AddParticipantAsync(classicMeetId, athleteASlug, 80.5m);
        await RecordAttemptAsync(classicMeetId, p3Id, Discipline.Squat, 1, ClassicSquatWeight);
        await RecordAttemptAsync(classicMeetId, p3Id, Discipline.Bench, 1, ClassicBenchWeight);
        await RecordAttemptAsync(classicMeetId, p3Id, Discipline.Deadlift, 1, ClassicDeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // --- Athlete B (junior) participations ---

        // Junior 83kg equipped meet (full powerlifting — all 3 disciplines)
        int p4Id = await AddParticipantAsync(juniorMeet83Id, athleteBSlug, 80.5m);
        await RecordAttemptAsync(juniorMeet83Id, p4Id, Discipline.Squat, 1, JuniorSquat83Weight);
        await RecordAttemptAsync(juniorMeet83Id, p4Id, Discipline.Bench, 1, JuniorBench83Weight);
        await RecordAttemptAsync(juniorMeet83Id, p4Id, Discipline.Deadlift, 1, JuniorDeadlift83Weight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Junior 74kg equipped meet (JuniorsOnly weight category, full powerlifting)
        int p5Id = await AddParticipantAsync(juniorMeet74Id, athleteBSlug, 70.0m);
        await RecordAttemptAsync(juniorMeet74Id, p5Id, Discipline.Squat, 1, JuniorSquat74Weight);
        await RecordAttemptAsync(juniorMeet74Id, p5Id, Discipline.Bench, 1, JuniorBench74Weight);
        await RecordAttemptAsync(juniorMeet74Id, p5Id, Discipline.Deadlift, 1, JuniorDeadlift74Weight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // --- SQL-only records (states that computation cannot produce) ---

        // Find an attempt ID from the equipped meet for SQL records that reference attempts
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        int benchAttemptId = await dbContext.Set<WebApi.Features.Attempts.Attempt>()
            .Where(a => a.ParticipationId == p2Id)
            .Where(a => a.Discipline == Discipline.Bench)
            .Select(a => a.AttemptId)
            .SingleAsync(TestContext.Current.CancellationToken);

        // Standard record: squat 93kg (open, male, equipped) — no AttemptId
        FormattableString standardSquatSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            OUTPUT INSERTED.RecordId
            VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 1, {StandardSquatWeight}, '2025-01-01', 1, NULL, 1, 0, 'test-setup')
            """;
        _sqlRecordIds.Add(await InsertRecordAsync(dbContext, standardSquatSql));

        // TotalWilks record — computation does not produce TotalWilks category
        FormattableString totalWilksSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            OUTPUT INSERTED.RecordId
            VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 7, {TotalWilksWeight}, '2025-03-15', 0, {benchAttemptId}, 1, 0, 'test-setup')
            """;
        _sqlRecordIds.Add(await InsertRecordAsync(dbContext, totalWilksSql));

        // TotalIpfPoints record — computation does not produce TotalIpfPoints category
        FormattableString totalIpfPointsSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            OUTPUT INSERTED.RecordId
            VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 8, {TotalIpfPointsWeight}, '2025-03-15', 0, {benchAttemptId}, 1, 0, 'test-setup')
            """;
        _sqlRecordIds.Add(await InsertRecordAsync(dbContext, totalIpfPointsSql));

        // Record for WC 105kg with no EraWeightCategory in current era
        FormattableString noEraWcSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            OUTPUT INSERTED.RecordId
            VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id105Kg}, 1, {NoEraWcWeight}, '2025-03-15', 0, {benchAttemptId}, 1, 0, 'test-setup')
            """;
        _sqlRecordIds.Add(await InsertRecordAsync(dbContext, noEraWcSql));

        // Corruption: bench 93kg, higher weight but IsCurrent=0
        FormattableString corruptBenchHigherSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            OUTPUT INSERTED.RecordId
            VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 2, {CorruptBenchHigherWeight}, '2025-06-01', 0, {benchAttemptId}, 0, 0, 'test-setup')
            """;
        _sqlRecordIds.Add(await InsertRecordAsync(dbContext, corruptBenchHigherSql));

        // Corruption: bench 93kg, lower weight but IsCurrent=1
        FormattableString corruptBenchLowerSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            OUTPUT INSERTED.RecordId
            VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 2, {CorruptBenchLowerWeight}, '2025-05-01', 0, {benchAttemptId}, 1, 0, 'test-setup')
            """;
        _sqlRecordIds.Add(await InsertRecordAsync(dbContext, corruptBenchLowerSql));

        // Corruption: BenchSingle 83kg with IsStandard=1 but linked to athlete via AttemptId
        FormattableString corruptIsStandardSql =
            $"""
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            OUTPUT INSERTED.RecordId
            VALUES ({TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 5, {CorruptIsStandardBenchSingleWeight}, '2025-03-15', 1, {benchAttemptId}, 1, 0, 'test-setup')
            """;
        _sqlRecordIds.Add(await InsertRecordAsync(dbContext, corruptIsStandardSql));
    }

    public async ValueTask DisposeAsync()
    {
        if (_meetIds.Count > 0)
        {
            await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
            ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

            string meetIdList = string.Join(", ", _meetIds);
            string sqlRecordIdList = string.Join(", ", _sqlRecordIds);
            string cleanupSql =
                $"""
                DELETE FROM Records WHERE AttemptId IN (
                    SELECT AttemptId FROM Attempts WHERE ParticipationId IN (
                        SELECT ParticipationId FROM Participations WHERE MeetId IN ({meetIdList})
                    )
                );
                DELETE FROM Records WHERE RecordId IN ({sqlRecordIdList});
                DELETE FROM Attempts WHERE ParticipationId IN (
                    SELECT ParticipationId FROM Participations WHERE MeetId IN ({meetIdList})
                );
                DELETE FROM Participations WHERE MeetId IN ({meetIdList});
                """;

            await dbContext.Database.ExecuteSqlRawAsync(cleanupSql);
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
        _httpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WithValidFilters()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsGroupedRecords()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.Count.ShouldBe(6);
        groups.ShouldContain(g => g.Category == "Hnébeygja");
        groups.ShouldContain(g => g.Category == "Bekkpressa");
        groups.ShouldContain(g => g.Category == "Réttstöðulyfta");
        groups.ShouldContain(g => g.Category == "Samtala");
    }

    [Fact]
    public async Task FiltersByGender_Male_ReturnsRecordsWithAthletes()
    {
        // Arrange — male open equipped has multiple records linked to athletes

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        int nonEmptyCount = groups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete != null);
        nonEmptyCount.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task FiltersByGender_Female_ReturnsNoRecordsWithAthletes()
    {
        // Arrange — female open equipped has no athlete-linked records

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=f&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        groups.SelectMany(g => g.Records).ShouldAllBe(r => r.Athlete == null);
    }

    [Fact]
    public async Task FiltersByAgeCategory_Junior_ReturnsOnlyJuniorRecords()
    {
        // Arrange — junior equipped male: 2 weight categories (83kg + 74kg JuniorsOnly)
        // Each produces 6 record categories (Squat, Bench, Deadlift, BenchSingle, DeadliftSingle, Total)

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=junior&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        int nonEmptyCount = groups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete != null);
        nonEmptyCount.ShouldBe(12);
    }

    [Fact]
    public async Task FiltersByEquipmentType_Classic()
    {
        // Arrange

        // Act
        List<RecordGroup>? classicGroups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=classic",
            CancellationToken.None);

        // Assert — at least one classic record with an athlete exists
        classicGroups.ShouldNotBeNull();
        classicGroups.ShouldNotBeEmpty();

        int classicNonEmptyCount = classicGroups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete != null);
        classicNonEmptyCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ReturnsAllRecordCategories_EvenWithNoRecords()
    {
        // Arrange — female junior equipped has no records but has active weight categories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=f&ageCategory=junior&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.Count.ShouldBe(6);
        groups.SelectMany(g => g.Records).ShouldAllBe(r => r.Athlete == null);
    }

    [Fact]
    public async Task IncludesStandardRecords()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldContain(r => r.IsStandard);
    }

    [Fact]
    public async Task ExcludesTotalWilksAndTotalIpfPoints()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotContain(g => g.Category == string.Empty);
    }

    [Fact]
    public async Task ExcludesWeightCategoriesNotInCurrentEra()
    {
        // Arrange — weight category 5 (105kg) has no EraWeightCategory row in current era

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "105");
    }

    [Fact]
    public async Task ExcludesJuniorsOnlyWeightCategories_ForNonJuniorAgeCategory()
    {
        // Arrange — weight category 4 (74kg) is JuniorsOnly

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "74");
    }

    [Fact]
    public async Task ReturnsHighestWeight_WhenMultipleRecordsExist()
    {
        // Arrange — squat 83kg has records at 190.0 (historical) and 200.0 (current)

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hnébeygja");
        RecordEntry squatRecord83 = squatGroup.Records.First(r => r.WeightCategory == "83");
        squatRecord83.Weight.ShouldBe(EquippedSquatWeight);
    }

    [Fact]
    public async Task SortsWeightCategoriesNumerically()
    {
        // Arrange — open equipped male has weight categories 83 and 93

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hnébeygja");
        List<string> weightCategories = squatGroup.Records.Select(r => r.WeightCategory).ToList();
        weightCategories.Count.ShouldBe(2);
        weightCategories.ShouldBe(
            weightCategories
                .OrderBy(w => decimal.Parse(w, System.Globalization.CultureInfo.InvariantCulture))
                .ToList());
    }

    [Fact]
    public async Task ReturnsHighestWeight_WhenIsCurrentFlagIsCorrupt()
    {
        // Arrange — bench record for 93kg: 150.0 (IsCurrent=0) vs 140.0 (IsCurrent=1)
        // The handler should return 150.0 (highest weight) regardless of IsCurrent flag

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup benchGroup = groups.First(g => g.Category == "Bekkpressa");
        RecordEntry benchRecord93 = benchGroup.Records.First(r => r.WeightCategory == "93");
        benchRecord93.Weight.ShouldBe(CorruptBenchHigherWeight);
    }

    [Fact]
    public async Task ReturnsIsStandardFalse_WhenRecordHasAthleteEvenIfDbFlagIsTrue()
    {
        // Arrange — BenchSingle 83kg has IsStandard=1 in DB but is linked to a real athlete via AttemptId

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup benchSingleGroup = groups.First(g => g.Category == "Bekkpressa (stök grein)");
        RecordEntry benchSingleRecord83 = benchSingleGroup.Records.First(r => r.WeightCategory == "83");
        benchSingleRecord83.Athlete.ShouldNotBeNull();
        benchSingleRecord83.IsStandard.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnsEmptyEntries_ForWeightCategoriesWithNoRecords()
    {
        // Arrange — 93kg has no deadlift record (equipped, open, male)

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup deadliftGroup = groups.First(g => g.Category == "Réttstöðulyfta");
        RecordEntry deadliftRecord93 = deadliftGroup.Records.First(r => r.WeightCategory == "93");
        deadliftRecord93.Athlete.ShouldBeNull();
        deadliftRecord93.Weight.ShouldBe(0m);
    }

    [Fact]
    public async Task ReturnsAllActiveWeightCategories_EvenWithNoRecords()
    {
        // Arrange — open equipped male: active weight categories are 83kg and 93kg

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();

        foreach (RecordGroup group in groups)
        {
            List<string> weightCategories = group.Records.Select(r => r.WeightCategory).ToList();
            weightCategories.ShouldContain("83");
            weightCategories.ShouldContain("93");
        }
    }

    private static async Task<int> InsertRecordAsync(ResultsDbContext dbContext, FormattableString sql)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
        }

        await using DbCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // Test-only code with hardcoded constant values, no user input
        command.CommandText = FormattableString.Invariant(sql);
#pragma warning restore CA2100
        object? result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        return (int)result!;
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender, DateOnly dateOfBirth)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Rc";

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

    private async Task<int> CreateMeetAndGetIdAsync(DateOnly startDate, bool isRaw)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsRaw(isRaw)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", CancellationToken.None);

        int meetId = meetDetails!.MeetId;
        _meetIds.Add(meetId);
        return meetId;
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

        return result!.ParticipationId;
    }

    private async Task RecordAttemptAsync(
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }
}