using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(RecordsCollection))]
public sealed class GetRecordsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/records";

    // Entity IDs — 2000+ range to avoid collisions with BackfillRecordsTests (500–1030)
    private const int AthleteId = 2000;
    private const int MeetId = 2000;
    private const int ParticipationId = 2000;
    private const int AttemptSquatId = 2000;
    private const int AttemptBenchId = 2001;
    private const int AttemptDeadliftId = 2002;

    // Record IDs
    private const int RecordSquatEquipped83 = 2001;
    private const int RecordBench83 = 2002;
    private const int RecordDeadlift83 = 2003;
    private const int RecordTotal83 = 2004;
    private const int RecordSquatClassic83 = 2005;
    private const int RecordStandardSquat93 = 2006;
    private const int RecordTotalWilks83 = 2007;
    private const int RecordTotalIpfPoints83 = 2008;
    private const int RecordJuniorSquat83 = 2009;
    private const int RecordLowerSquat83 = 2010;
    private const int RecordFemaleSquat63 = 2011;
    private const int RecordJuniorsOnlyWcOpen74 = 2012;
    private const int RecordJuniorsOnlyWcJunior74 = 2013;
    private const int RecordNoEraWc105 = 2014;
    private const int RecordCorruptBenchHigher93 = 2015;
    private const int RecordCorruptBenchLower93 = 2016;
    private const int RecordCorruptIsStandardBenchSingle83 = 2017;

    // Weight constants
    private const decimal EquippedSquatWeight = 200.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;
    private const decimal TotalWeight = 580.0m;
    private const decimal ClassicSquatWeight = 195.0m;
    private const decimal StandardSquatWeight = 220.0m;
    private const decimal TotalWilksWeight = 400.0m;
    private const decimal TotalIpfPointsWeight = 85.5m;
    private const decimal JuniorSquatWeight = 180.0m;
    private const decimal LowerSquatWeight = 190.0m;
    private const decimal FemaleSquatWeight = 120.0m;
    private const decimal JuniorsOnlyOpenWeight = 170.0m;
    private const decimal JuniorsOnlyJuniorWeight = 165.0m;
    private const decimal NoEraWcWeight = 230.0m;
    private const decimal CorruptBenchHigherWeight = 150.0m;
    private const decimal CorruptBenchLowerWeight = 140.0m;
    private const decimal CorruptIsStandardBenchSingleWeight = 130.0m;

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();

    public async ValueTask InitializeAsync()
    {
        // Athlete
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({AthleteId}, 'RecA', 'Test', '1985-07-02', 'm', {TestSeedConstants.Country.Id}, 'reca-test');
            SET IDENTITY_INSERT Athletes OFF;
            """);

        // Meet
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({MeetId}, 'GetRecords Meet', 'getrecords-meet', '2025-03-15', '2025-03-15', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1);
            SET IDENTITY_INSERT Meets OFF;
            """);

        // Participation
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({ParticipationId}, {AthleteId}, {MeetId}, 80.5, {TestSeedConstants.WeightCategory.Id83Kg}, {TestSeedConstants.AgeCategory.OpenId}, 1, 0, {EquippedSquatWeight}, {BenchWeight}, {DeadliftWeight}, {TotalWeight}, 400.0, 85.5, 1);
            SET IDENTITY_INSERT Participations OFF;
            """);

        // Attempts
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES
                ({AttemptSquatId}, {ParticipationId}, 1, 3, {EquippedSquatWeight}, 1, 'test-setup', 'test-setup'),
                ({AttemptBenchId}, {ParticipationId}, 2, 3, {BenchWeight}, 1, 'test-setup', 'test-setup'),
                ({AttemptDeadliftId}, {ParticipationId}, 3, 3, {DeadliftWeight}, 1, 'test-setup', 'test-setup');
            SET IDENTITY_INSERT Attempts OFF;
            """);

        // Records
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Records ON;
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES
                -- Equipped squat 83kg (open, male)
                ({RecordSquatEquipped83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {EquippedSquatWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Bench 83kg (open, male, equipped)
                ({RecordBench83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 2, {BenchWeight}, '2025-03-15', 0, {AttemptBenchId}, 1, 0, 'test-setup'),
                -- Deadlift 83kg (open, male, equipped)
                ({RecordDeadlift83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 3, {DeadliftWeight}, '2025-03-15', 0, {AttemptDeadliftId}, 1, 0, 'test-setup'),
                -- Total 83kg (open, male, equipped)
                ({RecordTotal83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 4, {TotalWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Classic squat 83kg (open, male)
                ({RecordSquatClassic83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {ClassicSquatWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 1, 'test-setup'),
                -- Standard record: squat 93kg (open, male, equipped), no AttemptId
                ({RecordStandardSquat93}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 1, {StandardSquatWeight}, '2025-01-01', 1, NULL, 1, 0, 'test-setup'),
                -- TotalWilks (should be excluded)
                ({RecordTotalWilks83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 7, {TotalWilksWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- TotalIpfPoints (should be excluded)
                ({RecordTotalIpfPoints83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 8, {TotalIpfPointsWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Junior squat 83kg (junior, male, equipped)
                ({RecordJuniorSquat83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.JuniorId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {JuniorSquatWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Lower squat 83kg (open, male, equipped) — beaten by 200.0
                ({RecordLowerSquat83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {LowerSquatWeight}, '2024-01-01', 0, {AttemptSquatId}, 0, 0, 'test-setup'),
                -- Female squat 63kg (open, female, equipped), no AttemptId
                ({RecordFemaleSquat63}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id63Kg}, 1, {FemaleSquatWeight}, '2025-03-15', 0, NULL, 1, 0, 'test-setup'),
                -- JuniorsOnly WC 74kg, open age category
                ({RecordJuniorsOnlyWcOpen74}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id74KgJunior}, 1, {JuniorsOnlyOpenWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- JuniorsOnly WC 74kg, junior age category
                ({RecordJuniorsOnlyWcJunior74}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.JuniorId}, {TestSeedConstants.WeightCategory.Id74KgJunior}, 1, {JuniorsOnlyJuniorWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Record for WC 105kg with no EraWeightCategory row in current era
                ({RecordNoEraWc105}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id105Kg}, 1, {NoEraWcWeight}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Corruption: bench 93kg, higher weight but IsCurrent=0
                ({RecordCorruptBenchHigher93}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 2, {CorruptBenchHigherWeight}, '2025-06-01', 0, {AttemptBenchId}, 0, 0, 'test-setup'),
                -- Corruption: bench 93kg, lower weight but IsCurrent=1
                ({RecordCorruptBenchLower93}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 2, {CorruptBenchLowerWeight}, '2025-05-01', 0, {AttemptBenchId}, 1, 0, 'test-setup'),
                -- Corruption: BenchSingle 83kg with IsStandard=1 but linked to athlete via AttemptId
                ({RecordCorruptIsStandardBenchSingle83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 5, {CorruptIsStandardBenchSingleWeight}, '2025-03-15', 1, {AttemptBenchId}, 1, 0, 'test-setup');
            SET IDENTITY_INSERT Records OFF;
            """);
    }

    public async ValueTask DisposeAsync()
    {
        // Delete in FK-safe reverse order
        await fixture.ExecuteSqlAsync(
            $"""
            DELETE FROM Records WHERE RecordId IN (
                {RecordSquatEquipped83},{RecordBench83},{RecordDeadlift83},{RecordTotal83},
                {RecordSquatClassic83},{RecordStandardSquat93},{RecordTotalWilks83},{RecordTotalIpfPoints83},
                {RecordJuniorSquat83},{RecordLowerSquat83},{RecordFemaleSquat63},
                {RecordJuniorsOnlyWcOpen74},{RecordJuniorsOnlyWcJunior74},{RecordNoEraWc105},
                {RecordCorruptBenchHigher93},{RecordCorruptBenchLower93},{RecordCorruptIsStandardBenchSingle83})
            """);

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Attempts WHERE AttemptId IN ({AttemptSquatId},{AttemptBenchId},{AttemptDeadliftId})");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Participations WHERE ParticipationId = {ParticipationId}");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Meets WHERE MeetId = {MeetId}");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Athletes WHERE AthleteId = {AthleteId}");

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
        groups.ShouldContain(g => g.Category == "Hn\u00e9beygja");
        groups.ShouldContain(g => g.Category == "Bekkpressa");
        groups.ShouldContain(g => g.Category == "R\u00e9ttst\u00f6\u00f0ulyfta");
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
        // Arrange — female open equipped has one standard record (no athlete)

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
        // Arrange — junior equipped male has 2 records with athletes (squat 83kg + JuniorsOnly 74kg)

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
        nonEmptyCount.ShouldBe(2);
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
        // Arrange — two squat records for (RecordCategoryId=1, WeightCategoryId=1): 200.0 and 190.0

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hn\u00e9beygja");
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
        RecordGroup squatGroup = groups.First(g => g.Category == "Hn\u00e9beygja");
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
        RecordGroup deadliftGroup = groups.First(g => g.Category == "R\u00e9ttst\u00f6\u00f0ulyfta");
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
}