using KRAFT.Results.WebApi;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ResultsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAthletes();
builder.Services.AddUsers(builder.Configuration);
builder.Services.AddTeams();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/healthz");
app.MapAthleteEndpoints();
app.MapTeamEndpoints();
app.MapUserEndpoints();

app.UseHttpsRedirection();

await app.RunAsync();