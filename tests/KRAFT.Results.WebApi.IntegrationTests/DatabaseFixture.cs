using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
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
            INSERT INTO AgeCategories (AgeCategoryId, Title)
            VALUES (1, 'Open');
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
        """);
    }
}