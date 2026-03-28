using KRAFT.Results.WebApi.IntegrationTests;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(IntegrationTestFixture))]

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public DatabaseFixture Database { get; private set; } = default!;

    public IntegrationTestFactory Factory { get; private set; } = default!;

    public HttpClient CreateAuthorizedHttpClient() =>
        Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, options => { });
            });
        })
        .CreateClient();

    public HttpClient CreateNoNameClaimHttpClient() =>
        Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestNoNameClaimAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestNoNameClaimAuthHandler>(
                    TestNoNameClaimAuthHandler.SchemeName, options => { });
            });
        })
        .CreateClient();

    public HttpClient CreateNonAdminAuthorizedHttpClient() =>
        Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestNonAdminAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestNonAdminAuthHandler>(
                    TestNonAdminAuthHandler.SchemeName, options => { });
            });
        })
        .CreateClient();

    public async ValueTask DisposeAsync()
    {
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