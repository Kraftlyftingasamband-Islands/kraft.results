using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(RecordsCollection))]
public sealed class GetRecordsByEraTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/records";

    // Entity IDs — 3000+ range to avoid collisions with GetRecordsTests (2000+) and BackfillRecordsTests (500-1030)
    private const int AthleteId = 3000;
    private const int MeetId = 3000;
    private const int ParticipationId = 3000;
    private const int AttemptSquatId = 3000;

    // Record IDs
    private const int RecordCurrentSquat83 = 3001;
    private const int RecordCurrentSquat93 = 3002;
    private const int RecordCurrentSquat105 = 3003;
    private const int RecordHistoricalSquat83 = 3004;
    private const int RecordHistoricalSquat105 = 3005;

    // Weight constants
    private const decimal CurrentSquatWeight83 = 210.0m;
    private const decimal CurrentSquatWeight93 = 225.0m;
    private const decimal CurrentSquatWeight105 = 240.0m;
    private const decimal HistoricalSquatWeight83 = 185.0m;
    private const decimal HistoricalSquatWeight105 = 260.0m;

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();

    public async ValueTask InitializeAsync()
    {
        // Athlete
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({AthleteId}, 'EraA', 'Test', '1985-07-02', 'm', {TestSeedConstants.Country.Id}, 'eraa-test');
            SET IDENTITY_INSERT Athletes OFF;
            """);

        // Meet
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({MeetId}, 'GetRecordsByEra Meet', 'getrecordsbyera-meet', '2025-03-15', '2025-03-15', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1);
            SET IDENTITY_INSERT Meets OFF;
            """);

        // Participation
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({ParticipationId}, {AthleteId}, {MeetId}, 80.5, {TestSeedConstants.WeightCategory.Id83Kg}, {TestSeedConstants.AgeCategory.OpenId}, 1, 0, {CurrentSquatWeight83}, 130.0, 250.0, 580.0, 400.0, 85.5, 1);
            SET IDENTITY_INSERT Participations OFF;
            """);

        // Attempt
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({AttemptSquatId}, {ParticipationId}, 1, 3, {CurrentSquatWeight83}, 1, 'test-setup', 'test-setup');
            SET IDENTITY_INSERT Attempts OFF;
            """);

        // Records
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Records ON;
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES
                -- Current era: squat 83kg (open, male, equipped)
                ({RecordCurrentSquat83}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {CurrentSquatWeight83}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Current era: squat 93kg (open, male, equipped) — verifies exclusion from historical era
                ({RecordCurrentSquat93}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 1, {CurrentSquatWeight93}, '2025-03-15', 1, NULL, 1, 0, 'test-setup'),
                -- Current era: squat 105kg (open, male, equipped) — verifies exclusion from current era (no EraWeightCategory)
                ({RecordCurrentSquat105}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id105Kg}, 1, {CurrentSquatWeight105}, '2025-03-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Historical era: squat 83kg (open, male, equipped)
                ({RecordHistoricalSquat83}, {TestSeedConstants.Era.HistoricalId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id83Kg}, 1, {HistoricalSquatWeight83}, '2017-06-15', 0, {AttemptSquatId}, 1, 0, 'test-setup'),
                -- Historical era: squat 105kg (open, male, equipped)
                ({RecordHistoricalSquat105}, {TestSeedConstants.Era.HistoricalId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id105Kg}, 1, {HistoricalSquatWeight105}, '2018-03-10', 0, {AttemptSquatId}, 1, 0, 'test-setup');
            SET IDENTITY_INSERT Records OFF;
            """);
    }

    public async ValueTask DisposeAsync()
    {
        // Delete in FK-safe reverse order
        await fixture.ExecuteSqlAsync(
            $"""
            DELETE FROM Records WHERE RecordId IN (
                {RecordCurrentSquat83},{RecordCurrentSquat93},{RecordCurrentSquat105},
                {RecordHistoricalSquat83},{RecordHistoricalSquat105})
            """);

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Attempts WHERE AttemptId IN ({AttemptSquatId})");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Participations WHERE ParticipationId = {ParticipationId}");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Meets WHERE MeetId = {MeetId}");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Athletes WHERE AthleteId = {AthleteId}");

        _httpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsCurrentEraRecords_WhenNoEraSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        groups.ShouldContain(g => g.Category == "Hn\u00e9beygja");
    }

    [Fact]
    public async Task ReturnsHistoricalEraRecords_WhenEraSlugSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hn\u00e9beygja");
        squatGroup.Records.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsHistoricalEraRecords_WithCorrectWeightCategories()
    {
        // Arrange — historical era has 83kg and 105kg weight categories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldContain(r => r.WeightCategory == "83");
        allRecords.ShouldContain(r => r.WeightCategory == "105");
    }

    [Fact]
    public async Task DoesNotReturnCurrentEraRecords_ForHistoricalEra()
    {
        // Arrange — current era has 93kg records, historical does not

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "93");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenEraSlugIsUnknown()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=nonexistent-era",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsCurrentEraRecords_WhenExplicitCurrentEraSlugSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=current-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        groups.ShouldContain(g => g.Category == "Hn\u00e9beygja");
    }

    [Fact]
    public async Task HistoricalEraExcludes105kg_FromCurrentEra()
    {
        // Arrange — 105kg is NOT in current era's EraWeightCategories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "105");
    }
}