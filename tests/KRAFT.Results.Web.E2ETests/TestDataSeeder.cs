using System.Diagnostics.CodeAnalysis;

using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.Web.E2ETests;

internal static class TestDataSeeder
{
    public const int SeededMeetYear = TestSeedConstants.Meet.Year;
    public const string SeededMeetSlug = TestSeedConstants.Meet.Slug;

    internal static async Task SeedAsync(string connectionString)
    {
        await RunMigrationsAsync(connectionString);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await ExecuteSqlAsync(connection, BaseSeedSql.CleanupSql());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedCountry());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedUsersAndRoles());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedTeam());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedAthlete());
        await ExecuteSqlAsync(connection, BaseSeedSql.SetAthleteTeamSql());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedAgeCategories());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedWeightCategories());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedEras());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedEraWeightCategories());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedMeet());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedBaseParticipations());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedBaseAttempts());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedBaseRecords());
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "All SQL is composed from compile-time constants in BaseSeedSql")]
    private static async Task ExecuteSqlAsync(SqlConnection connection, string sql)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
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