using KRAFT.Results.WebApi.IntegrationTests;

[assembly: AssemblyFixture(typeof(IntegrationTestFixture))]

namespace KRAFT.Results.WebApi.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public DatabaseFixture Database { get; private set; } = default!;

    public IntegrationTestFactory Factory { get; private set; } = default!;

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