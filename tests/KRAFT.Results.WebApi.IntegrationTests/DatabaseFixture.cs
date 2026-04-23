using System.Diagnostics.CodeAnalysis;

using KRAFT.Results.Tests.Shared;

using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace KRAFT.Results.WebApi.IntegrationTests;

[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "All SQL is composed from compile-time constants in BaseSeedSql and local const fields")]
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const int NorwayCountryId = 2;
    private const int AlphaTeamId = 2;
    private const int BetaTeamId = 3;
    private const int AnnaAthleteId = 2;
    private const int BobAthleteId = 3;
    private const int FemaleParticipationId = 4;
    private const int TcMeet1Id = 2;
    private const int TcMeet2Id = 3;
    private const int TcMeet2026Meet1Id = 4;
    private const int TcMeet2026Meet2Id = 5;
    private const int NoRecordsMeetId = 9;
    private const int DeadliftMeetId = 10;
    private const int DeadliftMeetTypeId = 3;
    private const int BannedAthleteId = 12;
    private const int EditorRoleId = 2;
    private const int UserRoleId = 3;

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        DbContextOptions<ResultsDbContext> options = new DbContextOptionsBuilder<ResultsDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using ResultsDbContext dbContext = new(options);

        await dbContext.Database.MigrateAsync();

        await SeedBaseDataAsync(dbContext);
        await SeedTeamCompetitionDataAsync(dbContext);
        await SeedIntegrationTestAttemptsAsync(dbContext);
        await SeedBestNTestDataAsync(dbContext);

        await SeedNoRecordsMeetAsync(dbContext);
        await SeedDeadliftMeetAsync(dbContext);
        await SeedBanDataAsync(dbContext);
        await SeedIntegrationRolesAsync(dbContext);
    }

    private static async Task SeedBaseDataAsync(ResultsDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedCountry());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedUsersAndRoles());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedTeam());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedAthlete());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SetAthleteTeamSql());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedAgeCategories());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedWeightCategories());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedEras());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedEraWeightCategories());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedMeet());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedBaseParticipations());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedBaseAttempts());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedBaseRecords());
    }

    private static async Task SeedTeamCompetitionDataAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            INSERT INTO Countries (CountryId, ISO2, ISO3, Name)
            VALUES ({NorwayCountryId}, 'NO', 'NOR', 'Norway');

            SET IDENTITY_INSERT Teams ON;
            INSERT INTO Teams (TeamId, Title, TitleShort, TitleFull, CountryId, Slug)
            VALUES ({AlphaTeamId}, 'Alpha Team', 'ALP', 'Alpha Team', {NorwayCountryId}, '{Constants.TeamCompetition.AlphaTeamSlug}');
            INSERT INTO Teams (TeamId, Title, TitleShort, TitleFull, CountryId, Slug)
            VALUES ({BetaTeamId}, 'Beta Team', 'BET', 'Beta Team', {NorwayCountryId}, '{Constants.TeamCompetition.BetaTeamSlug}');
            SET IDENTITY_INSERT Teams OFF;

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({AnnaAthleteId}, 'Anna', 'Test', '1990-01-01', 'f', {NorwayCountryId}, 'anna-test');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({BobAthleteId}, 'Bob', 'Test', '1988-05-10', 'm', {NorwayCountryId}, 'bob-test');
            SET IDENTITY_INSERT Athletes OFF;

            -- Female participation in test meet (used by ApproveRecordTests for isolation)
            -- Inserted here (before TC participations) to ensure it gets ParticipationId = 4
            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES ({FemaleParticipationId}, {AnnaAthleteId}, {TestSeedConstants.Meet.Id}, 61.5, 3, 1, 1, 0, 110.0, 70.0, 130.0, 310.0, 250.0, 55.0, 99);
            SET IDENTITY_INSERT Participations OFF;

            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({TcMeet1Id}, 'TC Meet 1 2025', '{Constants.TeamCompetition.TcMeet12025Slug}', '2025-06-01', '2025-06-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({TcMeet2Id}, 'TC Meet 2 2025', 'tc-meet-2-2025', '2025-09-01', '2025-09-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);
            SET IDENTITY_INSERT Meets OFF;

            -- Alpha Team male participations
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({BobAthleteId}, {TcMeet1Id}, 82.0, 1, {AlphaTeamId}, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.5, 1, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({BobAthleteId}, {TcMeet2Id}, 82.0, 1, {AlphaTeamId}, 1, 2, 0, 190.0, 120.0, 240.0, 550.0, 380.0, 80.0, 2, 9);

            -- Beta Team male participations
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({BobAthleteId}, {TcMeet1Id}, 82.0, 1, {BetaTeamId}, 1, 3, 0, 180.0, 110.0, 230.0, 520.0, 360.0, 75.0, 3, 8);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({BobAthleteId}, {TcMeet2Id}, 82.0, 1, {BetaTeamId}, 1, 1, 0, 210.0, 135.0, 260.0, 605.0, 420.0, 90.0, 1, 12);

            -- Alpha Team female participations
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({AnnaAthleteId}, {TcMeet1Id}, 60.0, 3, {AlphaTeamId}, 1, 1, 0, 100.0, 60.0, 120.0, 280.0, 300.0, 65.0, 4, 12);

            -- Beta Team female participations
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({AnnaAthleteId}, {TcMeet1Id}, 60.0, 3, {BetaTeamId}, 1, 2, 0, 90.0, 55.0, 110.0, 255.0, 280.0, 60.0, 5, 9);

            -- Disqualified participation (should be excluded)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({BobAthleteId}, {TcMeet1Id}, 82.0, 1, {AlphaTeamId}, 1, 4, 1, 170.0, 100.0, 220.0, 490.0, 340.0, 70.0, 6, 7);

            -- Participation with no team (should be excluded)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({BobAthleteId}, {TcMeet1Id}, 82.0, 1, 1, 5, 0, 160.0, 95.0, 210.0, 465.0, 320.0, 65.0, 7, 6);

            -- Participation with zero team points (should be excluded)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({BobAthleteId}, {TcMeet1Id}, 82.0, 1, {AlphaTeamId}, 1, 6, 0, 150.0, 90.0, 200.0, 440.0, 300.0, 60.0, 8, 0);
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedIntegrationTestAttemptsAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            SET IDENTITY_INSERT Attempts ON;
            -- Attempt 4: record-breaking squat (210 > current record of 200 for classic/open/83kg/male)
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (4, 1, 1, 2, 210.0, 1, 'seed', 'seed');

            -- Attempt 5: non-record-breaking squat (190 <= current record of 200 for equipped/open/83kg/male)
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (5, 1, 1, 1, 190.0, 1, 'seed', 'seed');

            -- Attempt 6: female squat for open/63kg raw slot (no existing raw record for this slot)
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (6, {FemaleParticipationId}, 1, 3, 110.0, 1, 'seed', 'seed');

            -- Attempt 7: zero-weight good bench attempt
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (7, 1, 2, 1, 0.0, 1, 'seed', 'seed');
            SET IDENTITY_INSERT Attempts OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedBestNTestDataAsync(ResultsDbContext dbContext)
    {
        const int tc1Id = 4;
        const int tc2Id = 5;
        const int tc3Id = 6;
        const int tc4Id = 7;
        const int tc5Id = 8;
        const int tc6Id = 9;

        string sql =
            $"""
            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({tc1Id}, 'TC1', 'Test', '1990-01-01', 'm', {NorwayCountryId}, 'tc1-test');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({tc2Id}, 'TC2', 'Test', '1990-01-01', 'm', {NorwayCountryId}, 'tc2-test');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({tc3Id}, 'TC3', 'Test', '1990-01-01', 'm', {NorwayCountryId}, 'tc3-test');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({tc4Id}, 'TC4', 'Test', '1990-01-01', 'm', {NorwayCountryId}, 'tc4-test');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({tc5Id}, 'TC5', 'Test', '1990-01-01', 'm', {NorwayCountryId}, 'tc5-test');
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({tc6Id}, 'TC6', 'Test', '1990-01-01', 'm', {NorwayCountryId}, 'tc6-test');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({TcMeet2026Meet1Id}, 'TC Meet 1 2026', '{Constants.TeamCompetition.TcMeet12026Slug}', '2026-06-01', '2026-06-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({TcMeet2026Meet2Id}, 'TC Meet 2 2026', 'tc-meet-2-2026', '2026-09-01', '2026-09-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);
            SET IDENTITY_INSERT Meets OFF;

            -- Alpha Team: 6 male athletes in meet1 2026, all scoring 12
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc1Id}, {TcMeet2026Meet1Id}, 82.0, 1, {AlphaTeamId}, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.0, 1, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc2Id}, {TcMeet2026Meet1Id}, 82.0, 1, {AlphaTeamId}, 1, 2, 0, 195.0, 125.0, 245.0, 565.0, 390.0, 83.0, 2, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc3Id}, {TcMeet2026Meet1Id}, 82.0, 1, {AlphaTeamId}, 1, 3, 0, 190.0, 120.0, 240.0, 550.0, 380.0, 81.0, 3, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc4Id}, {TcMeet2026Meet1Id}, 82.0, 1, {AlphaTeamId}, 1, 4, 0, 185.0, 115.0, 235.0, 535.0, 370.0, 79.0, 4, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc5Id}, {TcMeet2026Meet1Id}, 82.0, 1, {AlphaTeamId}, 1, 5, 0, 180.0, 110.0, 230.0, 520.0, 360.0, 77.0, 5, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc6Id}, {TcMeet2026Meet1Id}, 82.0, 1, {AlphaTeamId}, 1, 6, 0, 175.0, 105.0, 225.0, 505.0, 350.0, 75.0, 6, 12);

            -- Alpha Team: 3 male athletes in meet2 2026 scoring 12, 9, 8
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc1Id}, {TcMeet2026Meet2Id}, 82.0, 1, {AlphaTeamId}, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.0, 1, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc2Id}, {TcMeet2026Meet2Id}, 82.0, 1, {AlphaTeamId}, 1, 2, 0, 190.0, 120.0, 240.0, 550.0, 380.0, 81.0, 2, 9);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES ({tc3Id}, {TcMeet2026Meet2Id}, 82.0, 1, {AlphaTeamId}, 1, 3, 0, 185.0, 115.0, 235.0, 535.0, 370.0, 79.0, 3, 8);
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedNoRecordsMeetAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({NoRecordsMeetId}, 'No Records Meet', '{Constants.NoRecordsMeet.Slug}', '2025-12-01', '2025-12-01', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 1);
            SET IDENTITY_INSERT Meets OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedDeadliftMeetAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({DeadliftMeetId}, 'Réttstakeppni 2025', '{Constants.DeadliftMeet.Slug}', '2025-06-01', '2025-06-01', 1, 1, 1, 1, {DeadliftMeetTypeId}, 0, 1, 0, 1, 0, 1, 1);
            SET IDENTITY_INSERT Meets OFF;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedBanDataAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({BannedAthleteId}, 'Banned', 'Athlete', '1990-01-01', 'm', {NorwayCountryId}, '{Constants.BannedAthlete.Slug}');
            SET IDENTITY_INSERT Athletes OFF;

            INSERT INTO Bans (AthleteId, FromDate, ToDate)
            VALUES ({BannedAthleteId}, '2025-01-01', '2025-12-31');
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedIntegrationRolesAsync(ResultsDbContext dbContext)
    {
        string sql =
            $"""
            INSERT INTO Roles (RoleId, RoleName)
            VALUES ({EditorRoleId}, 'Editor'), ({UserRoleId}, 'User');
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}