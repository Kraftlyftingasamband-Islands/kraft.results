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
        builder.ConfigureServices(async services =>
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

            ServiceProvider provider = services.BuildServiceProvider();
            ResultsDbContext dbContext = provider.GetRequiredService<ResultsDbContext>();

            dbContext.Database.EnsureCreated();
            dbContext.Database.Migrate();
            dbContext.Database.ExecuteSqlRaw("""
                    INSERT INTO "Countries" (CountryId, ISO2, ISO3, Name)
                    VALUES (1, 'IS', 'ISL', 'Iceland')
                """);
            dbContext.Database.ExecuteSqlRaw("""
                    INSERT INTO "Users" (Username, Password)
                    VALUES ('testuser', 'TestPassword123!')
                """);
        });
    }
}