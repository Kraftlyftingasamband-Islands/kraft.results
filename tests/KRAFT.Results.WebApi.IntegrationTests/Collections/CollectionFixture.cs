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

[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "SQL is composed from compile-time constants (BaseSeedSql, local consts) and a GUID-derived database name containing only hex characters")]
public sealed class CollectionFixture : IAsyncLifetime
{
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
        string masterConnectionString = new SqlConnectionStringBuilder(_sqlServer.ConnectionString)
        {
            InitialCatalog = "master",
        }.ConnectionString;

        string databaseName = $"kraft_test_{Guid.NewGuid():N}";

        await using (SqlConnection masterConnection = new(masterConnectionString))
        {
            await masterConnection.OpenAsync();

            await using SqlCommand createCommand = new($"CREATE DATABASE [{databaseName}]", masterConnection);
            await createCommand.ExecuteNonQueryAsync();
        }

        _databaseConnectionString = new SqlConnectionStringBuilder(_sqlServer.ConnectionString)
        {
            InitialCatalog = databaseName,
        }.ConnectionString;

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
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO Roles (RoleId, RoleName)
            VALUES (2, 'Editor'), (3, 'User');
            """);
    }
}