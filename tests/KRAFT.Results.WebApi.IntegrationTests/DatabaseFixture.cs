using Testcontainers.MsSql;

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }
}