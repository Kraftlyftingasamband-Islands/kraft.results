using KRAFT.Results.WebApi;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.Web.E2ETests;

internal static class TestDataSeeder
{
    internal static async Task SeedAsync(string connectionString)
    {
        await RunMigrationsAsync(connectionString);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await using SqlCommand cleanupCommand = connection.CreateCommand();
        cleanupCommand.CommandText = """
            DELETE FROM Records;
            DELETE FROM Attempts;
            DELETE FROM Participations;
            DELETE FROM EraWeightCategories;
            DELETE FROM Photos;
            DELETE FROM Meets;
            DELETE FROM AthleteAliases;
            DELETE FROM Bans;
            DELETE FROM Athletes;
            DELETE FROM Teams;
            DELETE FROM Pages;
            DELETE FROM PageGroups;
            DELETE FROM Eras;
            DELETE FROM UserRoles;
            DELETE FROM Users;
            DELETE FROM Roles;
            DELETE FROM WeightCategories;
            DELETE FROM AgeCategories;
            DELETE FROM MeetTypes;
            DELETE FROM Countries;
            """;
        await cleanupCommand.ExecuteNonQueryAsync();

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Countries (CountryId, ISO2, ISO3, Name)
            VALUES (1, 'IS', 'ISL', 'Iceland');

            INSERT INTO Users (Username, Password, Email)
            VALUES ('testuser', 'testuser', 'test@email.com');

            INSERT INTO Roles (RoleId, RoleName)
            VALUES (1, 'Admin');

            INSERT INTO UserRoles (UserId, RoleId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
            SELECT u.UserId, 1, GETUTCDATE(), 'seed', GETUTCDATE(), 'seed'
            FROM Users u
            WHERE u.Username = 'testuser';

            INSERT INTO MeetTypes (MeetTypeId, Title)
            Values (1, 'Powerlifting');

            SET IDENTITY_INSERT Teams ON;
            INSERT INTO Teams (TeamId, Title, TitleShort, TitleFull, CountryId, Slug)
            VALUES (1, 'Test team', 'TTM', 'Test team', 1, 'test-team');
            SET IDENTITY_INSERT Teams OFF;

            SET IDENTITY_INSERT Athletes ON;
            INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, TeamId, Slug)
            VALUES (1, 'Testie', 'McTestFace', '1985-07-02', 'm', 1, 1, 'testie-mctestface');
            SET IDENTITY_INSERT Athletes OFF;

            SET IDENTITY_INSERT AgeCategories ON;
            INSERT INTO AgeCategories (AgeCategoryId, Title)
            VALUES (1, 'Open');
            SET IDENTITY_INSERT AgeCategories OFF;

            SET IDENTITY_INSERT WeightCategories ON;
            INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
            VALUES (1, '83', 74.01, 83.00, 'm', 0, '83-m');
            SET IDENTITY_INSERT WeightCategories OFF;

            SET IDENTITY_INSERT Meets ON;
            INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES (1, 'Test Meet 2025', 'test-meet-2025', '2025-03-15', '2025-03-15', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1);
            SET IDENTITY_INSERT Meets OFF;

            SET IDENTITY_INSERT Participations ON;
            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (1, 1, 1, 80.5, 1, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.5, 1);

            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (2, 1, 1, 80.5, 1, 1, 2, 0, 180.0, 120.0, 230.0, 550.0, 370.0, 75.0, 2);

            INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (3, 1, 1, 80.5, 1, 1, 3, 1, 180.0, 120.0, 230.0, 530.0, 360.0, 78.0, 3);
            SET IDENTITY_INSERT Participations OFF;

            UPDATE AgeCategories SET Slug = 'open' WHERE AgeCategoryId = 1;

            SET IDENTITY_INSERT AgeCategories ON;
            INSERT INTO AgeCategories (AgeCategoryId, Title, Slug)
            VALUES (2, 'Junior', 'junior');
            SET IDENTITY_INSERT AgeCategories OFF;

            SET IDENTITY_INSERT WeightCategories ON;
            INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
            VALUES (2, '93', 83.01, 93.00, 'm', 0, '93-m');
            INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
            VALUES (3, '63', 57.01, 63.00, 'f', 0, '63-f');
            INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
            VALUES (4, '74', 66.01, 74.00, 'm', 1, '74-m-jr');
            INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
            VALUES (5, '105', 93.01, 105.00, 'm', 0, '105-m');
            SET IDENTITY_INSERT WeightCategories OFF;

            SET IDENTITY_INSERT Eras ON;
            INSERT INTO Eras (EraId, Title, StartDate, EndDate, Slug)
            VALUES (1, 'Current Era', '2019-01-01', '2099-12-31', 'current-era');
            SET IDENTITY_INSERT Eras OFF;

            SET IDENTITY_INSERT EraWeightCategories ON;
            INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (1, 1, 1, '2019-01-01', '2099-12-31');
            INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (2, 1, 2, '2019-01-01', '2099-12-31');
            INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (3, 1, 3, '2019-01-01', '2099-12-31');
            INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (4, 1, 4, '2019-01-01', '2099-12-31');
            SET IDENTITY_INSERT EraWeightCategories OFF;

            SET IDENTITY_INSERT Attempts ON;
            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (1, 1, 1, 3, 200.0, 1, 'seed', 'seed');

            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (2, 1, 2, 3, 130.0, 1, 'seed', 'seed');

            INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (3, 1, 3, 3, 250.0, 1, 'seed', 'seed');
            SET IDENTITY_INSERT Attempts OFF;

            SET IDENTITY_INSERT Records ON;
            -- Squat record (equipped, open, male)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (1, 1, 1, 1, 1, 200.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Bench record (equipped, open, male)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 1, 2, 130.0, '2025-03-15', 0, 2, 1, 0, 'seed');

            -- Deadlift record (equipped, open, male)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (3, 1, 1, 1, 3, 250.0, '2025-03-15', 0, 3, 1, 0, 'seed');

            -- Total record (equipped, open, male)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (4, 1, 1, 1, 4, 580.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Classic squat record (classic, open, male)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (5, 1, 1, 1, 1, 195.0, '2025-03-15', 0, 1, 1, 1, 'seed');

            -- Standard record (equipped, open, male, 93kg)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (6, 1, 1, 2, 1, 220.0, '2025-01-01', 1, NULL, 1, 0, 'seed');

            -- TotalWilks record (should be excluded)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (7, 1, 1, 1, 7, 400.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- TotalIpfPoints record (should be excluded)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (8, 1, 1, 1, 8, 85.5, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Record for junior category (equipped, junior, male)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (9, 1, 2, 1, 1, 180.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Lower-weight record (same group as 200.0 squat; should be beaten by highest weight)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (10, 1, 1, 1, 1, 190.0, '2024-01-01', 0, 1, 0, 0, 'seed');

            -- Female record (equipped, open, female)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (11, 1, 1, 3, 1, 120.0, '2025-03-15', 0, NULL, 1, 0, 'seed');

            -- JuniorsOnly weight category record (equipped, open, male, 74kg JuniorsOnly — should be excluded for open)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (12, 1, 1, 4, 1, 170.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- JuniorsOnly weight category record (equipped, junior, male, 74kg JuniorsOnly — should be included for junior)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (13, 1, 2, 4, 1, 165.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Record for weight category with no EraWeightCategory row (should be excluded)
            INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (14, 1, 1, 5, 1, 230.0, '2025-03-15', 0, 1, 1, 0, 'seed');
            SET IDENTITY_INSERT Records OFF;
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task RunMigrationsAsync(string connectionString)
    {
        DbContextOptions<ResultsDbContext> options = new DbContextOptionsBuilder<ResultsDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using ResultsDbContext dbContext = new(options);
        await dbContext.Database.MigrateAsync();
    }
}