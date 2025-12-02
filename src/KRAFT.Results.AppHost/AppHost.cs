using System.Net.Sockets;

using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Aspire will automatically populate the sql-password in user secrets
string sqlPassword = builder.Configuration.GetValue<string>("Parameters:sql-password")
    ?? throw new InvalidOperationException("No SQL password found");

IResourceBuilder<SqlServerServerResource> sql = builder
    .AddSqlServer("sql")
    .WithContainerName("kraft-sql")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword)
    .WithDataVolume("kraft-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint("tcp", endpoint =>
    {
        endpoint.TargetPort = 1433;
        endpoint.Port = 1433;
        endpoint.IsExternal = true;
        endpoint.Protocol = ProtocolType.Tcp;
    });

IResourceBuilder<SqlServerDatabaseResource> db = sql.AddDatabase("kraft-db");

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.KRAFT_Results_WebApi>("api")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.KRAFT_Results_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

DistributedApplication app = builder.Build();

await app.RunAsync();