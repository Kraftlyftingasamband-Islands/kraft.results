using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public IntegrationTestFactory(string connectionString)
    {
        _connectionString = connectionString;
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

        ServiceDescriptor? eventHandlerDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IDomainEventHandler<AttemptRecordedEvent>));

        if (eventHandlerDescriptor is not null)
        {
            services.Remove(eventHandlerDescriptor);
        }

        ServiceDescriptor? workerDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(RecordComputationWorker));

        if (workerDescriptor is not null)
        {
            services.Remove(workerDescriptor);
        }

        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        services.AddDbContext<ResultsDbContext>((IServiceProvider serviceProvider, DbContextOptionsBuilder options) =>
        {
            options.UseSqlServer(_connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventInterceptor>());
        });
    }
}