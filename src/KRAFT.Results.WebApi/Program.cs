using KRAFT.Results.WebApi;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Teams;

using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ResultsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAthletes();
builder.Services.AddTeams();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/healthz");
app.MapAthleteEndpoints();
app.MapTeamEndpoints();

app.UseHttpsRedirection();

await app.RunAsync();