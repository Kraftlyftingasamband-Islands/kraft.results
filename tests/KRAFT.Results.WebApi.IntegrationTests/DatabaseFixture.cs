using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
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

        await dbContext.Database.ExecuteSqlAsync($"""
            INSERT INTO Countries (CountryId, ISO2, ISO3, Name)
            VALUES (1, 'IS', 'ISL', {Constants.TestCountryName});

            INSERT INTO Users (Username, Password, Email)
            VALUES ({Constants.TestUser.Username}, {Constants.TestUser.Password}, {Constants.TestUser.Email});

            INSERT INTO MeetTypes (MeetTypeId, Title)
            Values (1, {Constants.TestMeetType});

            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ({Constants.TestAthleteFirstName}, {Constants.TestAthleteLastName}, {Constants.TestAthleteDateOfBirth}, 'm', 1, {Constants.TestAthleteSlug});

            INSERT INTO Teams (Title, TitleShort, TitleFull, CountryId, Slug)
            VALUES ('Test team', 'TTM', 'Test team', 1, {Constants.TestTeamSlug});

            SET IDENTITY_INSERT AgeCategories ON;
            INSERT INTO AgeCategories (AgeCategoryId, Title, TitleShort, Slug)
            VALUES (1, 'Open', 'Open', 'open');
            SET IDENTITY_INSERT AgeCategories OFF;

            SET IDENTITY_INSERT WeightCategories ON;
            INSERT INTO WeightCategories (WeightCategoryId, Title, MinWeight, MaxWeight, Gender, JuniorsOnly, Slug)
            VALUES (1, '83', 74.01, 83.00, 'm', 0, '83-m');
            SET IDENTITY_INSERT WeightCategories OFF;

            INSERT INTO Meets (Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ({Constants.TestMeetTitle}, {Constants.TestMeetSlug}, '2025-03-15', '2025-03-15', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1);

            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (1, 1, 80.5, 1, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.5, 1);

            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (1, 1, 80.5, 1, 1, 2, 0, 180.0, 120.0, 230.0, 550.0, 370.0, 75.0, 2);

            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (1, 1, 80.5, 1, 1, 3, 1, 180.0, 120.0, 230.0, 530.0, 360.0, 78.0, 3);

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

            INSERT INTO Eras (Title, StartDate, EndDate, Slug)
            VALUES ('Historical Era', '2011-01-01', '2018-12-31', 'historical-era');

            INSERT INTO Eras (Title, StartDate, EndDate, Slug)
            VALUES ('Current Era', '2019-01-01', '2099-12-31', 'current-era');

            INSERT INTO EraWeightCategories (EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (2, 1, '2019-01-01', '2099-12-31');
            INSERT INTO EraWeightCategories (EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (2, 2, '2019-01-01', '2099-12-31');
            INSERT INTO EraWeightCategories (EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (2, 3, '2019-01-01', '2099-12-31');
            INSERT INTO EraWeightCategories (EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (2, 4, '2019-01-01', '2099-12-31');

            -- Historical era weight categories (105kg was valid in historical era)
            INSERT INTO EraWeightCategories (EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (1, 1, '2011-01-01', '2018-12-31');
            INSERT INTO EraWeightCategories (EraId, WeightCategoryId, FromDate, ToDate)
            VALUES (1, 5, '2011-01-01', '2018-12-31');

            -- Attempts 1-3: standard attempts linked to records
            INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (1, 1, 3, 200.0, 1, 'seed', 'seed');

            INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (1, 2, 3, 130.0, 1, 'seed', 'seed');

            INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (1, 3, 3, 250.0, 1, 'seed', 'seed');

            -- Attempt 4: record-breaking squat (210 > current record of 200 for classic/open/83kg/male)
            INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (1, 1, 2, 210.0, 1, 'seed', 'seed');

            -- Attempt 5: non-record-breaking squat (190 <= current record of 200 for equipped/open/83kg/male)
            INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (1, 1, 1, 190.0, 1, 'seed', 'seed');

            -- Attempt 6: record-breaking bench for raw slot (no existing raw bench record, so any weight qualifies)
            -- Reserved exclusively for ApproveRecordTests to prevent cross-test contamination
            INSERT INTO Attempts (ParticipationId, DisciplineId, Round, Weight, Good, CreatedBy, ModifiedBy)
            VALUES (1, 2, 2, 145.0, 1, 'seed', 'seed');

            -- Squat record (equipped, open, male)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 1, 200.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Bench record (equipped, open, male)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 2, 130.0, '2025-03-15', 0, 2, 1, 0, 'seed');

            -- Deadlift record (equipped, open, male)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 3, 250.0, '2025-03-15', 0, 3, 1, 0, 'seed');

            -- Total record (equipped, open, male)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 4, 580.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Classic squat record (classic, open, male)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 1, 195.0, '2025-03-15', 0, 1, 1, 1, 'seed');

            -- Standard record (equipped, open, male, 93kg)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 2, 1, 220.0, '2025-01-01', 1, NULL, 1, 0, 'seed');

            -- TotalWilks record (should be excluded)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 7, 400.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- TotalIpfPoints record (should be excluded)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 8, 85.5, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Record for junior category (equipped, junior, male)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 2, 1, 1, 180.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Lower-weight record (same group as 200.0 squat; should be beaten by highest weight)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 1, 190.0, '2024-01-01', 0, 1, 0, 0, 'seed');

            -- Female record (equipped, open, female)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 3, 1, 120.0, '2025-03-15', 0, NULL, 1, 0, 'seed');

            -- JuniorsOnly weight category record (equipped, open, male, 74kg JuniorsOnly — should be excluded for open)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 4, 1, 170.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- JuniorsOnly weight category record (equipped, junior, male, 74kg JuniorsOnly — should be included for junior)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 2, 4, 1, 165.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Record for weight category with no EraWeightCategory row (should be excluded from current era)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 5, 1, 230.0, '2025-03-15', 0, 1, 1, 0, 'seed');

            -- Historical era records (era 1)
            -- Squat record in historical era (equipped, open, male, 83kg)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (1, 1, 1, 1, 185.0, '2017-06-15', 0, 1, 1, 0, 'seed');

            -- Squat record in historical era (equipped, open, male, 105kg — valid in historical era)
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (1, 1, 5, 1, 260.0, '2018-03-10', 0, 1, 1, 0, 'seed');

            -- Team Competition seed data
            INSERT INTO Countries (CountryId, ISO2, ISO3, Name)
            VALUES (2, 'NO', 'NOR', 'Norway');

            INSERT INTO Teams (Title, TitleShort, TitleFull, CountryId, Slug)
            VALUES ('Alpha Team', 'ALP', 'Alpha Team', 2, {Constants.TeamCompetition.AlphaTeamSlug});

            INSERT INTO Teams (Title, TitleShort, TitleFull, CountryId, Slug)
            VALUES ('Beta Team', 'BET', 'Beta Team', 2, {Constants.TeamCompetition.BetaTeamSlug});

            -- Female athlete for gender-split testing (non-IS country to avoid ranking interference)
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('Anna', 'Test', '1990-01-01', 'f', 2, 'anna-test');

            -- Male athlete 2 (non-IS country to avoid ranking interference)
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('Bob', 'Test', '1988-05-10', 'm', 2, 'bob-test');

            -- Team competition meet (year 2025, gender split applies)
            INSERT INTO Meets (Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ('TC Meet 1 2025', 'tc-meet-1-2025', '2025-06-01', '2025-06-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);

            INSERT INTO Meets (Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ('TC Meet 2 2025', 'tc-meet-2-2025', '2025-09-01', '2025-09-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);

            -- Alpha Team male participations (5 entries across two meets, best 5 used)
            -- Meet 2 (MeetId will be the tc-meet-1 id), Alpha = TeamId 2, male athlete (AthleteId 1 from seed is male)
            -- Using athlete 3 (Bob, male) for team competition
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (3, 2, 82.0, 1, 2, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.5, 1, 12);

            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (3, 3, 82.0, 1, 2, 1, 2, 0, 190.0, 120.0, 240.0, 550.0, 380.0, 80.0, 2, 9);

            -- Beta Team male participations
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (3, 2, 82.0, 1, 3, 1, 3, 0, 180.0, 110.0, 230.0, 520.0, 360.0, 75.0, 3, 8);

            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (3, 3, 82.0, 1, 3, 1, 1, 0, 210.0, 135.0, 260.0, 605.0, 420.0, 90.0, 1, 12);

            -- Alpha Team female participations
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (2, 2, 60.0, 3, 2, 1, 1, 0, 100.0, 60.0, 120.0, 280.0, 300.0, 65.0, 4, 12);

            -- Beta Team female participations
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (2, 2, 60.0, 3, 3, 1, 2, 0, 90.0, 55.0, 110.0, 255.0, 280.0, 60.0, 5, 9);

            -- Disqualified participation (should be excluded)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (3, 2, 82.0, 1, 2, 1, 4, 1, 170.0, 100.0, 220.0, 490.0, 340.0, 70.0, 6, 7);

            -- Participation with no team (should be excluded)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (3, 2, 82.0, 1, 1, 5, 0, 160.0, 95.0, 210.0, 465.0, 320.0, 65.0, 7, 6);

            -- Participation with zero team points (should be excluded)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (3, 2, 82.0, 1, 2, 1, 6, 0, 150.0, 90.0, 200.0, 440.0, 300.0, 60.0, 8, 0);

            -- BestN per-meet test data (year 2026)
            -- Additional male athletes for bestN testing
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('TC1', 'Test', '1990-01-01', 'm', 2, 'tc1-test');
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('TC2', 'Test', '1990-01-01', 'm', 2, 'tc2-test');
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('TC3', 'Test', '1990-01-01', 'm', 2, 'tc3-test');
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('TC4', 'Test', '1990-01-01', 'm', 2, 'tc4-test');
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('TC5', 'Test', '1990-01-01', 'm', 2, 'tc5-test');
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('TC6', 'Test', '1990-01-01', 'm', 2, 'tc6-test');

            -- Two TC meets in 2026
            INSERT INTO Meets (Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ('TC Meet 1 2026', 'tc-meet-1-2026', '2026-06-01', '2026-06-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);

            INSERT INTO Meets (Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ('TC Meet 2 2026', 'tc-meet-2-2026', '2026-09-01', '2026-09-01', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1);

            -- Alpha Team: 6 male athletes in meet1 2026, all scoring 12 (MeetId=4, TeamId=2)
            -- Athletes 4-9 are TC1-TC6
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (4, 4, 82.0, 1, 2, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.0, 1, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (5, 4, 82.0, 1, 2, 1, 2, 0, 195.0, 125.0, 245.0, 565.0, 390.0, 83.0, 2, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (6, 4, 82.0, 1, 2, 1, 3, 0, 190.0, 120.0, 240.0, 550.0, 380.0, 81.0, 3, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (7, 4, 82.0, 1, 2, 1, 4, 0, 185.0, 115.0, 235.0, 535.0, 370.0, 79.0, 4, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (8, 4, 82.0, 1, 2, 1, 5, 0, 180.0, 110.0, 230.0, 520.0, 360.0, 77.0, 5, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (9, 4, 82.0, 1, 2, 1, 6, 0, 175.0, 105.0, 225.0, 505.0, 350.0, 75.0, 6, 12);

            -- Alpha Team: 3 male athletes in meet2 2026 scoring 12, 9, 8 (MeetId=5, TeamId=2)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (4, 5, 82.0, 1, 2, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.0, 1, 12);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (5, 5, 82.0, 1, 2, 1, 2, 0, 190.0, 120.0, 240.0, 550.0, 380.0, 81.0, 2, 9);
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, TeamId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo, TeamPoints)
            VALUES (6, 5, 82.0, 1, 2, 1, 3, 0, 185.0, 115.0, 235.0, 535.0, 370.0, 79.0, 3, 8);

            -- Ordering test: athletes with controlled names for alphabetical sorting
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('Delta', 'Test', '1992-01-01', 'm', 2, 'delta-test');
            INSERT INTO Athletes (Firstname, Lastname, DateOfBirth, Gender, CountryId, Slug)
            VALUES ('Charlie', 'Test', '1993-01-01', 'm', 2, 'charlie-test');

            -- Ordering test meet (MeetId=6)
            INSERT INTO Meets (Title, Slug, StartDate, EndDate, CalcPlaces, PublishedResults, ResultModeId, IsRaw, MeetTypeId, IsInTeamCompetition, ShowWilks, ShowTeamPoints, ShowBodyWeight, ShowTeams, RecordsPossible, PublishedInCalendar)
            VALUES ('Ordering Meet 2025', {Constants.OrderingMeet.Slug}, '2025-10-01', '2025-10-01', 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 1);

            -- Participation: Place=1, not DQ (Delta Test, AthleteId=10)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (10, 6, 82.0, 1, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.0, 1);

            -- Participation: Place=1 (tied), not DQ (Charlie Test, AthleteId=11)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (11, 6, 82.0, 1, 1, 1, 0, 200.0, 130.0, 250.0, 580.0, 400.0, 85.0, 2);

            -- Participation: Place=3, not DQ (Bob Test, AthleteId=3)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (3, 6, 82.0, 1, 1, 3, 0, 180.0, 120.0, 230.0, 530.0, 370.0, 78.0, 3);

            -- Participation: Place=-1, DQ (Anna Test, AthleteId=2)
            INSERT INTO Participations (AthleteId, MeetId, Weight, WeightCategoryId, AgeCategoryId, Place, Disqualified, Squat, Benchpress, Deadlift, Total, Wilks, IPFPoints, LotNo)
            VALUES (2, 6, 82.0, 1, 1, -1, 1, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 4);

            -- IsCurrent corruption test data: bench record for 93kg where IsCurrent=1 is on a LOWER weight
            -- The higher-weight row (150.0) has IsCurrent=0, the lower-weight row (140.0) has IsCurrent=1
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 2, 2, 150.0, '2025-06-01', 0, 2, 0, 0, 'seed');

            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 2, 2, 140.0, '2025-05-01', 0, 2, 1, 0, 'seed');

            -- IsStandard flag corruption: record with IsStandard=1 but linked to a real athlete via AttemptId
            -- BenchSingle (RecordCategoryId=5) for 83kg equipped open male
            INSERT INTO Records (EraId, AgeCategoryId, WeightCategoryId, RecordCategoryId, Weight, Date, IsStandard, AttemptId, IsCurrent, IsRaw, CreatedBy)
            VALUES (2, 1, 1, 5, 130.0, '2025-03-15', 1, 2, 1, 0, 'seed');
        """);
    }
}