using System.Net.Sockets;

using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Aspire will automatically populate the sql-password in user secrets
string sqlPassword = builder.Configuration.GetValue<string>("Parameters:sql-password")
    ?? throw new InvalidOperationException("No SQL password found");

string dataVolume = builder.Configuration.GetValue<string>("SqlServer:DataVolume") ?? "kraft-data";
string containerName = builder.Configuration.GetValue<string>("SqlServer:ContainerName") ?? "kraft-sql";
int sqlPort = builder.Configuration.GetValue<int?>("SqlServer:Port") ?? 1433;
bool persistentLifetime = builder.Configuration.GetValue<bool?>("SqlServer:Persistent") ?? true;

IResourceBuilder<SqlServerServerResource> sql = builder
    .AddSqlServer("sql")
    .WithContainerName(containerName)
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword)
    .WithDataVolume(dataVolume)
    .WithLifetime(persistentLifetime ? ContainerLifetime.Persistent : ContainerLifetime.Session)
    .WithEndpoint("tcp", endpoint =>
    {
        endpoint.TargetPort = 1433;
        endpoint.Port = sqlPort;
        endpoint.IsExternal = true;
        endpoint.Protocol = ProtocolType.Tcp;
    });

IResourceBuilder<SqlServerDatabaseResource> db = sql.AddDatabase("kraft-db");

string? rateLimitPermit = builder.Configuration.GetValue<string>("RateLimiting:Auth:PermitLimit");

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.KRAFT_Results_WebApi>("api")
    .WithReference(db)
    .WaitFor(db);

if (rateLimitPermit is not null)
{
    api = api.WithEnvironment("RateLimiting__Auth__PermitLimit", rateLimitPermit);
}

builder.AddProject<Projects.KRAFT_Results_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints()
    .WithEnvironment("API__BaseAddress", api.GetEndpoint("http"));

DistributedApplication app = builder.Build();

await app.RunAsync();