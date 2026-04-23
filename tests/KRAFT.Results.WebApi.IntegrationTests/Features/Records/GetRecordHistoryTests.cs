using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(RecordsCollection))]
public sealed class GetRecordHistoryTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string RecordsPath = "/records";

    // Entity IDs — 4000+ range to avoid collisions with GetRecordsByEraTests (3000+)
    private const int AthleteId = 4000;
    private const int MeetId = 4000;
    private const int ParticipationId = 4000;
    private const int AttemptId = 4000;

    // Record IDs — a chain of 3 records in the same group (era, ageCategory, weightCategory, recordCategory)
    private const int RecordOldest = 4001;
    private const int RecordMiddle = 4002;
    private const int RecordCurrent = 4003;

    // Weight constants
    private const decimal OldestWeight = 180.0m;
    private const decimal MiddleWeight = 195.0m;
    private const decimal CurrentWeight = 210.0m;

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();

    public async ValueTask InitializeAsync()
    {
        // Athlete
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({AthleteId}, 'HistA', 'Test', '1985-07-02', 'm', {TestSeedConstants.Country.Id}, 'hista-test');
            SET IDENTITY_INSERT Athletes OFF;
            """);

        // Meet
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({MeetId}, 'GetRecordHistory Meet', 'getrecordhistory-meet', '2025-03-15', '2025-03-15', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1);
            SET IDENTITY_INSERT Meets OFF;
            """);

        // Participation
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({ParticipationId}, {AthleteId}, {MeetId}, 80.5, {TestSeedConstants.WeightCategory.Id83Kg}, {TestSeedConstants.AgeCategory.OpenId}, 1, 0, {CurrentWeight}, 130.0, 250.0, 590.0, 400.0, 85.5, 1);
            SET IDENTITY_INSERT Participations OFF;
            """);

        // Attempt
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES ({AttemptId}, {ParticipationId}, 1, 3, {CurrentWeight}, 1, 'test-setup', 'test-setup');
            SET IDENTITY_INSERT Attempts OFF;
            """);

        // Records — 3 records in the same group (classic squat 93kg, open), different dates, one current
        // Uses IsRaw=1 + WeightCategory 93kg to avoid collisions with other test classes
        await fixture.ExecuteSqlAsync(
            $"""
            SET IDENTITY_INSERT Records ON;
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES
                -- Oldest record (not current)
                ({RecordOldest}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 1, {OldestWeight}, '2023-06-10', 0, {AttemptId}, 0, 1, 'test-setup'),
                -- Middle record (not current)
                ({RecordMiddle}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 1, {MiddleWeight}, '2024-03-15', 0, {AttemptId}, 0, 1, 'test-setup'),
                -- Current record
                ({RecordCurrent}, {TestSeedConstants.Era.CurrentId}, {TestSeedConstants.AgeCategory.OpenId}, {TestSeedConstants.WeightCategory.Id93Kg}, 1, {CurrentWeight}, '2025-03-15', 0, {AttemptId}, 1, 1, 'test-setup');
            SET IDENTITY_INSERT Records OFF;
            """);
    }

    public async ValueTask DisposeAsync()
    {
        // Delete in FK-safe reverse order
        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Records WHERE RecordId IN ({RecordOldest},{RecordMiddle},{RecordCurrent})");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Attempts WHERE AttemptId IN ({AttemptId})");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Participations WHERE ParticipationId = {ParticipationId}");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Meets WHERE MeetId = {MeetId}");

        await fixture.ExecuteSqlAsync(
            $"DELETE FROM Athletes WHERE AthleteId = {AthleteId}");

        _httpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk_WithValidRecordId()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{RecordsPath}/{RecordCurrent}/history",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsNotFound_WithInvalidRecordId()
    {
        // Arrange
        int invalidId = 999999;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{RecordsPath}/{invalidId}/history",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsEntriesOrderedByDate()
    {
        // Arrange

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{RecordCurrent}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Entries.ShouldNotBeEmpty();

        List<DateOnly> dates = history.Entries
            .Select(e => e.Date)
            .ToList();

        dates.ShouldBe(dates.OrderBy(d => d).ToList());
    }

    [Fact]
    public async Task CurrentRecordIsMarked()
    {
        // Arrange

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{RecordCurrent}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Entries.Count(e => e.IsCurrent).ShouldBe(1);
    }

    [Fact]
    public async Task ResponseIncludesMetadata()
    {
        // Arrange

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{RecordCurrent}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Category.ShouldNotBeNullOrWhiteSpace();
        history.WeightCategory.ShouldNotBeNullOrWhiteSpace();
        history.AgeCategory.ShouldNotBeNullOrWhiteSpace();
        history.Gender.ShouldNotBeNullOrWhiteSpace();
        history.EquipmentType.ShouldNotBeNullOrWhiteSpace();
    }
}