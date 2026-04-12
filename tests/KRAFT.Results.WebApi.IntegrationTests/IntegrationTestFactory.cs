using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _databaseFixture;

    public IntegrationTestFactory(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");
        builder.ConfigureServices(ReplaceDbContext);
    }

    private void ReplaceDbContext(IServiceCollection services)
    {
        ServiceDescriptor? descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<ResultsDbContext>));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        ServiceDescriptor? backfillDescriptor = services.SingleOrDefault(
            d => d.ImplementationType == typeof(BackfillRecordsJob));

        if (backfillDescriptor is not null)
        {
            services.Remove(backfillDescriptor);
        }

        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        services.AddDbContext<ResultsDbContext>((IServiceProvider serviceProvider, DbContextOptionsBuilder options) =>
        {
            options.UseSqlServer(_databaseFixture.ConnectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventInterceptor>());
        });
    }
}