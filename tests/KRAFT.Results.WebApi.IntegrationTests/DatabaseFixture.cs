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

        await dbContext.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Countries (CountryId, ISO2, ISO3, Name)
            VALUES (1, 'IS', 'ISL', 'Iceland');

            INSERT INTO Users (Username, Password, Email)
            VALUES ('{Constants.TestUsername}', '{Constants.TestPassword}', '{Constants.TestEmail}');

            INSERT INTO MeetTypes (MeetTypeId, Title)
            Values (1, '{Constants.TestMeetType}');
        
            INSERT INTO Athletes (Firstname, Lastname, Gender, CountryId, Slug)
            VALUES ('Testie', 'McTestFace', 'm', 1, '{Constants.TestAthleteSlug}');
        """);
    }
}