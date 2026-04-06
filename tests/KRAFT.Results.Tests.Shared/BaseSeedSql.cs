namespace KRAFT.Results.Tests.Shared;

/// <summary>
/// Returns raw SQL strings for seeding base test data.
/// Methods must be called in FK-safe order:
/// <see cref="SeedCountry"/> → <see cref="SeedUsersAndRoles"/> → <see cref="SeedMeetType"/>
/// → <see cref="SeedTeam"/> → <see cref="SeedAthlete"/> → <see cref="SeedAgeCategories"/>
/// → <see cref="SeedWeightCategories"/> → <see cref="SeedEras"/> → <see cref="SeedEraWeightCategories"/>
/// → <see cref="SeedMeet"/> → <see cref="SeedBaseParticipations"/> → <see cref="SeedBaseAttempts"/>
/// → <see cref="SeedBaseRecords"/>.
/// </summary>
public static class BaseSeedSql
{
    public static string SeedCountry() =>
        $"""
        SET IDENTITY_INSERT Countries ON;
        INSERT INTO Countries (CountryId, ISO2, ISO3, Name)
        VALUES ({TestSeedConstants.Country.Id}, '{TestSeedConstants.Country.ISO2}', '{TestSeedConstants.Country.ISO3}', '{TestSeedConstants.Country.Name}');
        SET IDENTITY_INSERT Countries OFF;
        """;

    public static string SeedUsersAndRoles() =>
        $"""
        INSERT INTO Users (Username, Password, Email)
        VALUES ('{TestSeedConstants.User.Username}', '{TestSeedConstants.User.Password}', '{TestSeedConstants.User.Email}');

        INSERT INTO Roles (RoleId, RoleName)
        VALUES ({TestSeedConstants.Role.AdminId}, '{TestSeedConstants.Role.AdminName}');

        INSERT INTO UserRoles (UserId, RoleId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
        SELECT u.UserId, {TestSeedConstants.Role.AdminId}, GETUTCDATE(), 'seed', GETUTCDATE(), 'seed'
        FROM Users u
        WHERE u.Username = '{TestSeedConstants.User.Username}';
        """;

    public static string SeedMeetType() =>
        $"""
        INSERT INTO MeetTypes (MeetTypeId, Title)
        VALUES ({TestSeedConstants.MeetType.Id}, '{TestSeedConstants.MeetType.Title}');
        """;

    public static string SeedTeam() =>
        $"""
        SET IDENTITY_INSERT Teams ON;
        INSERT INTO Teams (TeamId, Title, TitleShort, TitleFull, CountryId, Slug)
        VALUES ({TestSeedConstants.Team.Id}, '{TestSeedConstants.Team.Title}', '{TestSeedConstants.Team.TitleShort}', '{TestSeedConstants.Team.TitleFull}', {TestSeedConstants.Country.Id}, '{TestSeedConstants.Team.Slug}');
        SET IDENTITY_INSERT Teams OFF;
        """;

    public static string SeedAthlete() =>
        $"""
        SET IDENTITY_INSERT Athletes ON;
        INSERT INTO Athletes (AthleteId, Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
        VALUES ({TestSeedConstants.Athlete.Id}, '{TestSeedConstants.Athlete.FirstName}', '{TestSeedConstants.Athlete.LastName}', '{TestSeedConstants.Athlete.DateOfBirth:yyyy-MM-dd}', '{TestSeedConstants.Athlete.Gender}', {TestSeedConstants.Country.Id}, '{TestSeedConstants.Athlete.Slug}');
        SET IDENTITY_INSERT Athletes OFF;
        """;

    public static string SeedAgeCategories() =>
        """
        SET IDENTITY_INSERT AgeCategories ON;
        INSERT INTO AgeCategories (AgeCategoryId, Title, TitleShort, Slug)
        VALUES (1, 'Open', 'Open', 'open');
        SET IDENTITY_INSERT AgeCategories OFF;

        UPDATE AgeCategories SET Slug = 'open' WHERE AgeCategoryId = 1;

        SET IDENTITY_INSERT AgeCategories ON;
        INSERT INTO AgeCategories (AgeCategoryId, Title, Slug)
        VALUES (2, 'Junior', 'junior');
        SET IDENTITY_INSERT AgeCategories OFF;
        """;

    public static string SeedWeightCategories() =>
        """
        SET IDENTITY_INSERT WeightCategories ON;
        INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
        VALUES (1, '83', 74.01, 83.00, 'm', 0, '83-m');
        INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
        VALUES (2, '93', 83.01, 93.00, 'm', 0, '93-m');
        INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
        VALUES (3, '63', 57.01, 63.00, 'f', 0, '63-f');
        INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
        VALUES (4, '74', 66.01, 74.00, 'm', 1, '74-m-jr');
        INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
        VALUES (5, '105', 93.01, 105.00, 'm', 0, '105-m');
        SET IDENTITY_INSERT WeightCategories OFF;
        """;

    public static string SeedEras() =>
        """
        SET IDENTITY_INSERT Eras ON;
        INSERT INTO Eras (EraId, Title, StartDate, EndDate, Slug)
        VALUES (1, 'Historical Era', '2011-01-01', '2018-12-31', 'historical-era');
        INSERT INTO Eras (EraId, Title, StartDate, EndDate, Slug)
        VALUES (2, 'Current Era', '2019-01-01', '2099-12-31', 'current-era');
        SET IDENTITY_INSERT Eras OFF;
        """;

    public static string SeedEraWeightCategories() =>
        """
        SET IDENTITY_INSERT EraWeightCategories ON;
        INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
        VALUES (1, 2, 1, '2019-01-01', '2099-12-31');
        INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
        VALUES (2, 2, 2, '2019-01-01', '2099-12-31');
        INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
        VALUES (3, 2, 3, '2019-01-01', '2099-12-31');
        INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
        VALUES (4, 2, 4, '2019-01-01', '2099-12-31');
        INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
        VALUES (5, 1, 1, '2011-01-01', '2018-12-31');
        INSERT INTO EraWeightCategories (EraWeightCategoryId, EraId, WeightCategoryId, FromDate, ToDate)
        VALUES (6, 1, 5, '2011-01-01', '2018-12-31');
        SET IDENTITY_INSERT EraWeightCategories OFF;
        """;

    public static string SeedMeet() =>
        $"""
        SET IDENTITY_INSERT Meets ON;
        INSERT INTO Meets (MeetId, Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
        VALUES ({TestSeedConstants.Meet.Id}, '{TestSeedConstants.Meet.Title}', '{TestSeedConstants.Meet.Slug}', '2025-03-15', '2025-03-15', 1, 1, 1, 1, {TestSeedConstants.MeetType.Id}, 0, 1, 0, 1, 0, 1, 1);
        SET IDENTITY_INSERT Meets OFF;
        """;

    public static string SeedBaseParticipations() =>
        """
        SET IDENTITY_INSERT Participations ON;
        INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
        VALUES (1, 1, 1, 80.5, 1, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.5, 1);

        INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
        VALUES (2, 1, 1, 80.5, 1, 1, 2, 0, 180.0, 120.0, 230.0, 550.0, 370.0, 75.0, 2);

        INSERT INTO Participations (ParticipationId, AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
        VALUES (3, 1, 1, 80.5, 1, 1, 3, 1, 180.0, 120.0, 230.0, 530.0, 360.0, 78.0, 3);
        SET IDENTITY_INSERT Participations OFF;
        """;

    public static string SeedBaseAttempts() =>
        """
        SET IDENTITY_INSERT Attempts ON;
        INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
        VALUES (1, 1, 1, 3, 200.0, 1, 'seed', 'seed');

        INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
        VALUES (2, 1, 2, 3, 130.0, 1, 'seed', 'seed');

        INSERT INTO Attempts (AttemptId, ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
        VALUES (3, 1, 3, 3, 250.0, 1, 'seed', 'seed');
        SET IDENTITY_INSERT Attempts OFF;
        """;

    public static string SeedBaseRecords() =>
        """
        SET IDENTITY_INSERT Records ON;
        -- Squat record (equipped, open, male)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (1, 2, 1, 1, 1, 200.0, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- Bench record (equipped, open, male)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (2, 2, 1, 1, 2, 130.0, '2025-03-15', 0, 2, 1, 0, 'seed');

        -- Deadlift record (equipped, open, male)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (3, 2, 1, 1, 3, 250.0, '2025-03-15', 0, 3, 1, 0, 'seed');

        -- Total record (equipped, open, male)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (4, 2, 1, 1, 4, 580.0, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- Classic squat record (classic, open, male)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (5, 2, 1, 1, 1, 195.0, '2025-03-15', 0, 1, 1, 1, 'seed');

        -- Standard record (equipped, open, male, 93kg)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (6, 2, 1, 2, 1, 220.0, '2025-01-01', 1, NULL, 1, 0, 'seed');

        -- TotalWilks record (should be excluded)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (7, 2, 1, 1, 7, 400.0, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- TotalIpfPoints record (should be excluded)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (8, 2, 1, 1, 8, 85.5, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- Record for junior category (equipped, junior, male)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (9, 2, 2, 1, 1, 180.0, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- Lower-weight record (same group as 200.0 squat; should be beaten by highest weight)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (10, 2, 1, 1, 1, 190.0, '2024-01-01', 0, 1, 0, 0, 'seed');

        -- Female record (equipped, open, female)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (11, 2, 1, 3, 1, 120.0, '2025-03-15', 0, NULL, 1, 0, 'seed');

        -- JuniorsOnly weight category record (equipped, open, male, 74kg JuniorsOnly)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (12, 2, 1, 4, 1, 170.0, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- JuniorsOnly weight category record (equipped, junior, male, 74kg JuniorsOnly)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (13, 2, 2, 4, 1, 165.0, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- Record for weight category with no EraWeightCategory row
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (14, 2, 1, 5, 1, 230.0, '2025-03-15', 0, 1, 1, 0, 'seed');

        -- Historical era: squat record (equipped, open, male, 83kg)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (15, 1, 1, 1, 1, 185.0, '2017-06-15', 0, 1, 1, 0, 'seed');

        -- Historical era: squat record (equipped, open, male, 105kg)
        INSERT INTO Records (RecordId, EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
        VALUES (16, 1, 1, 5, 1, 260.0, '2018-03-10', 0, 1, 1, 0, 'seed');
        SET IDENTITY_INSERT Records OFF;
        """;

    public static string CleanupSql() =>
        """
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
}