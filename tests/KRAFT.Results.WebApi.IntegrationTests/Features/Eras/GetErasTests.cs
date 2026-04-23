using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Eras;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Eras;

[Collection(nameof(InfraCollection))]
public sealed class GetErasTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/eras";
    private const string CreatedBy = "era-test";

    // Owned IDs (5000+ range)
    private const int OwnedAthleteId = 5000;
    private const int OwnedHistoricalMeetId = 5000;
    private const int OwnedCurrentMeetId = 5001;
    private const int OwnedHistoricalParticipationId = 5000;
    private const int OwnedCurrentParticipationId = 5001;
    private const int OwnedHistoricalAttemptId = 5000;
    private const int OwnedCurrentAttemptId = 5001;
    private const int OwnedHistoricalRecordId = 5000;
    private const int OwnedCurrentRecordId = 5001;

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            Path,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsAllEras()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            Path,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        eras.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ReturnsErasOrderedByStartDate()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            Path,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        eras[0].Slug.ShouldBe("historical-era");
        eras[1].Slug.ShouldBe("current-era");
    }

    [Fact]
    public async Task ReturnsEraSummaryWithAllFields()
    {
        // Arrange

        // Act
        List<EraSummary>? eras = await _httpClient.GetFromJsonAsync<List<EraSummary>>(
            Path,
            CancellationToken.None);

        // Assert
        eras.ShouldNotBeNull();
        EraSummary historicalEra = eras.First(e => e.Slug == "historical-era");
        historicalEra.Title.ShouldBe("Historical Era");
        historicalEra.StartDate.ShouldBe(new DateOnly(2011, 1, 1));
        historicalEra.EndDate.ShouldBe(new DateOnly(2018, 12, 31));
    }

    public async ValueTask InitializeAsync()
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        string seedSql =
            $"""
            -- Athlete
            IF NOT EXISTS (SELECT 1 FROM Athletes WHERE AthleteId = {OwnedAthleteId})
            BEGIN
                SET IDENTITY_INSERT Athletes ON;
                INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
                VALUES ({OwnedAthleteId}, 'Era', 'Test', '1990-01-01', 'm', 1, 'era-test');
                SET IDENTITY_INSERT Athletes OFF;
            END

            -- Historical era meet (date within 2011-01-01 to 2018-12-31)
            IF NOT EXISTS (SELECT 1 FROM Meets WHERE MeetId = {OwnedHistoricalMeetId})
            BEGIN
                SET IDENTITY_INSERT Meets ON;
                INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
                VALUES ({OwnedHistoricalMeetId}, 'Era Historical Meet', 'era-historical-meet', '2017-06-15', '2017-06-15', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1);
                SET IDENTITY_INSERT Meets OFF;
            END

            -- Current era meet (date within 2019-01-01 to 2099-12-31)
            IF NOT EXISTS (SELECT 1 FROM Meets WHERE MeetId = {OwnedCurrentMeetId})
            BEGIN
                SET IDENTITY_INSERT Meets ON;
                INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
                VALUES ({OwnedCurrentMeetId}, 'Era Current Meet', 'era-current-meet', '2025-03-15', '2025-03-15', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1);
                SET IDENTITY_INSERT Meets OFF;
            END

            -- Historical participation
            IF NOT EXISTS (SELECT 1 FROM Participations WHERE ParticipationId = {OwnedHistoricalParticipationId})
            BEGIN
                SET IDENTITY_INSERT Participations ON;
                INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
                VALUES ({OwnedHistoricalParticipationId}, {OwnedAthleteId}, {OwnedHistoricalMeetId}, 80.5, 1, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.5, 1);
                SET IDENTITY_INSERT Participations OFF;
            END

            -- Current participation
            IF NOT EXISTS (SELECT 1 FROM Participations WHERE ParticipationId = {OwnedCurrentParticipationId})
            BEGIN
                SET IDENTITY_INSERT Participations ON;
                INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
                VALUES ({OwnedCurrentParticipationId}, {OwnedAthleteId}, {OwnedCurrentMeetId}, 80.5, 1, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.5, 1);
                SET IDENTITY_INSERT Participations OFF;
            END

            -- Historical attempt
            IF NOT EXISTS (SELECT 1 FROM Attempts WHERE AttemptId = {OwnedHistoricalAttemptId})
            BEGIN
                SET IDENTITY_INSERT Attempts ON;
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({OwnedHistoricalAttemptId}, {OwnedHistoricalParticipationId}, 1, 1, 200.0, 1, '{CreatedBy}', '{CreatedBy}');
                SET IDENTITY_INSERT Attempts OFF;
            END

            -- Current attempt
            IF NOT EXISTS (SELECT 1 FROM Attempts WHERE AttemptId = {OwnedCurrentAttemptId})
            BEGIN
                SET IDENTITY_INSERT Attempts ON;
                INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
                VALUES ({OwnedCurrentAttemptId}, {OwnedCurrentParticipationId}, 1, 1, 200.0, 1, '{CreatedBy}', '{CreatedBy}');
                SET IDENTITY_INSERT Attempts OFF;
            END

            -- Historical record (EraId=1, IsCurrent=1, IsRaw=1)
            IF NOT EXISTS (SELECT 1 FROM Records WHERE RecordId = {OwnedHistoricalRecordId})
            BEGIN
                SET IDENTITY_INSERT Records ON;
                INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
                VALUES ({OwnedHistoricalRecordId}, 1, 1, 1, 1, 200.0, '2017-06-15', 0, {OwnedHistoricalAttemptId}, 1, 1, '{CreatedBy}');
                SET IDENTITY_INSERT Records OFF;
            END

            -- Current record (EraId=2, IsCurrent=1, IsRaw=1)
            IF NOT EXISTS (SELECT 1 FROM Records WHERE RecordId = {OwnedCurrentRecordId})
            BEGIN
                SET IDENTITY_INSERT Records ON;
                INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
                VALUES ({OwnedCurrentRecordId}, 2, 1, 1, 1, 200.0, '2025-03-15', 0, {OwnedCurrentAttemptId}, 1, 1, '{CreatedBy}');
                SET IDENTITY_INSERT Records OFF;
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(seedSql);
    }

    public async ValueTask DisposeAsync()
    {
        await using AsyncServiceScope scope = fixture.Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();

        string cleanupSql =
            $"""
            DELETE FROM Records WHERE CreatedBy = '{CreatedBy}';
            DELETE FROM Attempts WHERE CreatedBy = '{CreatedBy}';
            DELETE FROM Participations WHERE ParticipationId IN ({OwnedHistoricalParticipationId}, {OwnedCurrentParticipationId});
            DELETE FROM Meets WHERE MeetId IN ({OwnedHistoricalMeetId}, {OwnedCurrentMeetId});
            DELETE FROM Athletes WHERE AthleteId = {OwnedAthleteId};
            """;

        await dbContext.Database.ExecuteSqlRawAsync(cleanupSql);

        _httpClient.Dispose();
    }
}