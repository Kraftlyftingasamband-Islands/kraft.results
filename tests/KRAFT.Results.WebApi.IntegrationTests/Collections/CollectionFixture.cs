using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KRAFT.Results.WebApi.IntegrationTests.Collections;

[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "All SQL is composed from compile-time constants in BaseSeedSql and local const fields")]
public sealed class CollectionFixture : IAsyncLifetime
{
    private const int EditorRoleId = 2;
    private const int UserRoleId = 3;

    private readonly SqlServerFixture _sqlServer;
    private readonly ConcurrentBag<WebApplicationFactory<Program>> _childFactories = [];
    private string _databaseConnectionString = string.Empty;

    public CollectionFixture(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public IntegrationTestFactory? Factory { get; private set; }

    public int ChildFactoryCount => _childFactories.Count;

    public HttpClient CreateAuthorizedHttpClient()
    {
        WebApplicationFactory<Program> childFactory = Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, options => { });
            });
        });

        _childFactories.Add(childFactory);
        return childFactory.CreateClient();
    }

    public HttpClient CreateNoNameClaimHttpClient()
    {
        WebApplicationFactory<Program> childFactory = Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestNoNameClaimAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestNoNameClaimAuthHandler>(
                    TestNoNameClaimAuthHandler.SchemeName, options => { });
            });
        });

        _childFactories.Add(childFactory);
        return childFactory.CreateClient();
    }

    public HttpClient CreateNonAdminAuthorizedHttpClient()
    {
        WebApplicationFactory<Program> childFactory = Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestNonAdminAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestNonAdminAuthHandler>(
                    TestNonAdminAuthHandler.SchemeName, options => { });
            });
        });

        _childFactories.Add(childFactory);
        return childFactory.CreateClient();
    }

    public async Task ExecuteSqlAsync(FormattableString sql)
    {
        await using AsyncServiceScope scope = Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        await dbContext.Database.ExecuteSqlAsync(sql);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (WebApplicationFactory<Program> childFactory in _childFactories)
        {
            await childFactory.DisposeAsync();
        }

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
    }

    public async ValueTask InitializeAsync()
    {
        SqlConnectionStringBuilder masterBuilder = new(_sqlServer.ConnectionString)
        {
            InitialCatalog = "master",
        };

        string databaseName = $"kraft_test_{Guid.NewGuid():N}"[..19];

        await using (SqlConnection masterConnection = new(masterBuilder.ConnectionString))
        {
            await masterConnection.OpenAsync();

            await using SqlCommand createCommand = new($"CREATE DATABASE [{databaseName}]", masterConnection);
            await createCommand.ExecuteNonQueryAsync();
        }

        SqlConnectionStringBuilder dbBuilder = new(_sqlServer.ConnectionString)
        {
            InitialCatalog = databaseName,
        };

        _databaseConnectionString = dbBuilder.ConnectionString;

        DbContextOptions<ResultsDbContext> options = new DbContextOptionsBuilder<ResultsDbContext>()
            .UseSqlServer(_databaseConnectionString)
            .Options;

        await using ResultsDbContext dbContext = new(options);

        await dbContext.Database.MigrateAsync();

        await SeedBaseDataAsync(dbContext);
        await SeedIntegrationRolesAsync(dbContext);

        Factory = new IntegrationTestFactory(_databaseConnectionString);
    }

    // internal: RecordComputationChannel is internal, so the return type cannot be public
    internal (HttpClient Client, RecordComputationChannel Channel) CreateAuthorizedHttpClientWithRecordComputation()
    {
        WebApplicationFactory<Program> childFactory = Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, options => { });

                services.AddScoped<IDomainEventHandler<AttemptRecordedEvent>, AttemptRecordedEventHandler>();
                services.AddHostedService<RecordComputationWorker>();
            });
        });

        _childFactories.Add(childFactory);
        RecordComputationChannel channel = childFactory.Services.GetRequiredService<RecordComputationChannel>();
        return (childFactory.CreateClient(), channel);
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