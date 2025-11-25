using KRAFT.Results.WebApi;
using KRAFT.Results.WebApi.Features.Athletes;

using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ResultsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAthletes();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/healthz");
app.MapAthleteEndpoints();

app.UseHttpsRedirection();

await app.RunAsync();