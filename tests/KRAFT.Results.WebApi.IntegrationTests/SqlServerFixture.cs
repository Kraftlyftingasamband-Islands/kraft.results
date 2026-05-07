using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(KRAFT.Results.WebApi.IntegrationTests.SqlServerFixture))]

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}