using System.Diagnostics.CodeAnalysis;

using KRAFT.Results.Tests.Shared;

using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace KRAFT.Results.WebApi.IntegrationTests;

[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "All SQL is composed from compile-time constants in BaseSeedSql and local const fields")]
public sealed class DatabaseFixture : IAsyncLifetime
{
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
        await SeedIntegrationRolesAsync(dbContext);
    }

    private static async Task SeedBaseDataAsync(ResultsDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedCountry());
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM Countries WHERE ISO3 = 'NOR')
                INSERT INTO Countries (CountryId, ISO2, ISO3, Name)
                VALUES (2, 'NO', 'NOR', 'Norway');
            """);
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedUsersAndRoles());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedTeam());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedAthlete());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SetAthleteTeamSql());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedAgeCategories());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedWeightCategories());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedEras());
        await dbContext.Database.ExecuteSqlRawAsync(BaseSeedSql.SeedEraWeightCategories());
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