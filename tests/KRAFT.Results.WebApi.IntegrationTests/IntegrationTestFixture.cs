using System.Collections.Concurrent;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(IntegrationTestFixture))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly ConcurrentBag<WebApplicationFactory<Program>> _childFactories = [];

    public DatabaseFixture Database { get; private set; } = default!;

    public IntegrationTestFactory Factory { get; private set; } = default!;

    public int ChildFactoryCount => _childFactories.Count;

    public HttpClient CreateAuthorizedHttpClient()
    {
        WebApplicationFactory<Program> childFactory = Factory.WithWebHostBuilder(builder =>
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
        WebApplicationFactory<Program> childFactory = Factory.WithWebHostBuilder(builder =>
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

    public HttpClient CreateAuthorizedHttpClientWithRecordComputation()
    {
        WebApplicationFactory<Program> childFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, options => { });

                services.AddScoped<IDomainEventHandler<AttemptRecordedEvent>, AttemptRecordedEventHandler>();
            });
        });

        _childFactories.Add(childFactory);
        return childFactory.CreateClient();
    }

    public HttpClient CreateNonAdminAuthorizedHttpClient()
    {
        WebApplicationFactory<Program> childFactory = Factory.WithWebHostBuilder(builder =>
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
}