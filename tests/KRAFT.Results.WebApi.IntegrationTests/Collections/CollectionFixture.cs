using System.Collections.Concurrent;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KRAFT.Results.WebApi.IntegrationTests.Collections;

public sealed class CollectionFixture : IAsyncLifetime
{
    private readonly ConcurrentBag<WebApplicationFactory<Program>> _childFactories = [];

    public DatabaseFixture? Database { get; private set; }

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

    public async Task ExecuteSqlAsync(string sql)
    {
        await using AsyncServiceScope scope = Factory!.Services.CreateAsyncScope();
        ResultsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResultsDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (WebApplicationFactory<Program> childFactory in _childFactories)
        {
            await childFactory.DisposeAsync();
        }

        if (Database is not null)
        {
            await Database.DisposeAsync();
        }

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
    }

    public async ValueTask InitializeAsync()
    {
        Database = new DatabaseFixture();
        await Database.InitializeAsync();

        Factory = new IntegrationTestFactory(Database);
    }

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
}