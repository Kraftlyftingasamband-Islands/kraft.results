using Microsoft.AspNetCore.Authentication;
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
        builder.ConfigureServices(services =>
        {
            ReplaceDbContext(services);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, options => { });
        });
    }

    private void ReplaceDbContext(IServiceCollection services)
    {
        ServiceDescriptor? descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<ResultsDbContext>));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<ResultsDbContext>(options =>
        {
            options.UseSqlServer(_databaseFixture.ConnectionString);
        });
    }
}