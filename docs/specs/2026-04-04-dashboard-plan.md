# Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the empty home page with a data-rich dashboard showing season stats, upcoming/recent meets, IPF rankings, recent records, and team competition standings.

**Architecture:** Single `GET /dashboard` endpoint (anonymous) returns a `DashboardSummary` contract. The Blazor WebAssembly component `DashboardPage.razor` lives in `Web.Client` with `@page "/"` and replaces the currently-empty `Home.razor`. All data is fetched in one request.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core 10, Blazor WebAssembly, xUnit 3, Shouldly, SQL Server via Testcontainers.

---

## File Map

**Create:**
- `src/KRAFT.Results.Contracts/Dashboard/DashboardSummary.cs` — all contract types for this feature
- `src/KRAFT.Results.WebApi/Features/Dashboard/GetDashboard/GetDashboardHandler.cs` — query logic
- `src/KRAFT.Results.WebApi/Features/Dashboard/GetDashboard/GetDashboardEndpoint.cs` — `GET /dashboard`
- `src/KRAFT.Results.WebApi/Features/Dashboard/DashboardEndpoints.cs` — route group registration
- `src/KRAFT.Results.WebApi/Features/Dashboard/DashboardServices.cs` — DI registration
- `src/KRAFT.Results.Web.Client/Features/Dashboard/DashboardPage.razor` — the home page component
- `src/KRAFT.Results.Web.Client/Features/Dashboard/DashboardPage.razor.css` — scoped styles
- `tests/KRAFT.Results.WebApi.IntegrationTests/Features/Dashboard/GetDashboardTests.cs` — integration tests

**Modify:**
- `src/KRAFT.Results.WebApi/Features/FeatureServices.cs` — register `AddDashboard()`
- `src/KRAFT.Results.WebApi/Features/FeatureEndpoints.cs` — register `MapDashboardEndpoints()`

**Delete:**
- `src/KRAFT.Results.Web/Components/Pages/Home.razor` — replaced by `DashboardPage.razor` in Web.Client
- `src/KRAFT.Results.Web/Components/Pages/Home.razor.css` — unused

---

## Task 1: Contracts

**Files:**
- Create: `src/KRAFT.Results.Contracts/Dashboard/DashboardSummary.cs`

- [ ] **Step 1: Create the contract file**

```csharp
using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.Contracts.Dashboard;

public sealed record DashboardSummary(
    DashboardSeasonStats SeasonStats,
    IReadOnlyList<MeetSummary> RecentMeets,
    IReadOnlyList<MeetSummary> UpcomingMeets,
    IReadOnlyList<DashboardRankingEntry> TopRankingsMen,
    IReadOnlyList<DashboardRankingEntry> TopRankingsWomen,
    IReadOnlyList<DashboardRecordEntry> RecentRecordsMen,
    IReadOnlyList<DashboardRecordEntry> RecentRecordsWomen,
    IReadOnlyList<DashboardTeamEntry> TeamStandingsMen,
    IReadOnlyList<DashboardTeamEntry> TeamStandingsWomen
);

public sealed record DashboardSeasonStats(
    int Meets,
    int Athletes,
    int Records,
    int Clubs
);

public sealed record DashboardRankingEntry(
    string AthleteSlug,
    string AthleteName,
    string WeightCategory,
    decimal IpfPoints
);

public sealed record DashboardRecordEntry(
    string Lift,
    string AthleteSlug,
    string AthleteName,
    string WeightCategory,
    string AgeCategory,
    bool IsClassic,
    decimal Weight,
    string MeetSlug,
    DateOnly MeetDate
);

public sealed record DashboardTeamEntry(
    string TeamSlug,
    string TeamName,
    int Points
);
```

- [ ] **Step 2: Build to verify contracts compile**

Run: `dotnet build src/KRAFT.Results.Contracts`  
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/KRAFT.Results.Contracts/Dashboard/DashboardSummary.cs
git commit -m "feat(dashboard): add contract types"
```

---

## Task 2: Failing integration test

**Files:**
- Create: `tests/KRAFT.Results.WebApi.IntegrationTests/Features/Dashboard/GetDashboardTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Dashboard;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Dashboard;

public sealed class GetDashboardTests(IntegrationTestFixture fixture)
{
    private const string Path = "/dashboard";

    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(Path, CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        result.ShouldNotBeNull();
        result.RecentMeets.ShouldNotBeNull();
        result.UpcomingMeets.ShouldNotBeNull();
        result.TopRankingsMen.ShouldNotBeNull();
        result.TopRankingsWomen.ShouldNotBeNull();
        result.RecentRecordsMen.ShouldNotBeNull();
        result.RecentRecordsWomen.ShouldNotBeNull();
        result.TeamStandingsMen.ShouldNotBeNull();
        result.TeamStandingsWomen.ShouldNotBeNull();
    }

    [Fact]
    public async Task SeasonStats_AreNonNegative()
    {
        DashboardSummary? result = await _client.GetFromJsonAsync<DashboardSummary>(Path, CancellationToken.None);

        result.ShouldNotBeNull();
        result.SeasonStats.Meets.ShouldBeGreaterThanOrEqualTo(0);
        result.SeasonStats.Athletes.ShouldBeGreaterThanOrEqualTo(0);
        result.SeasonStats.Records.ShouldBeGreaterThanOrEqualTo(0);
        result.SeasonStats.Clubs.ShouldBeGreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 2: Run to confirm it fails**

Run: `dotnet test --filter "FullyQualifiedName~GetDashboardTests"`  
Expected: FAIL — `404 Not Found` (endpoint doesn't exist yet).

---

## Task 3: Backend — handler, endpoint, wiring

**Files:**
- Create: `src/KRAFT.Results.WebApi/Features/Dashboard/GetDashboard/GetDashboardHandler.cs`
- Create: `src/KRAFT.Results.WebApi/Features/Dashboard/GetDashboard/GetDashboardEndpoint.cs`
- Create: `src/KRAFT.Results.WebApi/Features/Dashboard/DashboardEndpoints.cs`
- Create: `src/KRAFT.Results.WebApi/Features/Dashboard/DashboardServices.cs`
- Modify: `src/KRAFT.Results.WebApi/Features/FeatureServices.cs`
- Modify: `src/KRAFT.Results.WebApi/Features/FeatureEndpoints.cs`

- [ ] **Step 1: Create the handler**

```csharp
using KRAFT.Results.Contracts.Dashboard;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.TeamCompetition;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

internal sealed class GetDashboardHandler(ResultsDbContext dbContext)
{
    public async Task<DashboardSummary> Handle(CancellationToken cancellationToken)
    {
        int currentYear = DateTime.UtcNow.Year;
        DateTime today = DateTime.UtcNow.Date;

        DashboardSeasonStats stats = await GetSeasonStatsAsync(currentYear, cancellationToken);

        List<MeetSummary> recentMeets = await dbContext.Set<Meet>()
            .Where(m => m.PublishedResults)
            .OrderByDescending(m => m.StartDate)
            .Take(3)
            .Select(m => new MeetSummary(m.Slug, m.Title, m.Location, DateOnly.FromDateTime(m.StartDate)))
            .ToListAsync(cancellationToken);

        List<MeetSummary> upcomingMeets = await dbContext.Set<Meet>()
            .Where(m => m.PublishedInCalendar && m.StartDate > today)
            .OrderBy(m => m.StartDate)
            .Take(3)
            .Select(m => new MeetSummary(m.Slug, m.Title, m.Location, DateOnly.FromDateTime(m.StartDate)))
            .ToListAsync(cancellationToken);

        List<DashboardRankingEntry> rankingsMen = await GetTopRankingsAsync("m", currentYear, cancellationToken);
        List<DashboardRankingEntry> rankingsWomen = await GetTopRankingsAsync("f", currentYear, cancellationToken);

        List<DashboardRecordEntry> recordsMen = await GetRecentRecordsAsync("m", cancellationToken);
        List<DashboardRecordEntry> recordsWomen = await GetRecentRecordsAsync("f", cancellationToken);

        (List<DashboardTeamEntry> teamsMen, List<DashboardTeamEntry> teamsWomen) =
            await GetTeamStandingsAsync(currentYear, cancellationToken);

        return new DashboardSummary(
            stats,
            recentMeets,
            upcomingMeets,
            rankingsMen,
            rankingsWomen,
            recordsMen,
            recordsWomen,
            teamsMen,
            teamsWomen);
    }

    private async Task<DashboardSeasonStats> GetSeasonStatsAsync(int year, CancellationToken cancellationToken)
    {
        int meets = await dbContext.Set<Meet>()
            .Where(m => m.PublishedResults && m.StartDate.Year == year)
            .CountAsync(cancellationToken);

        int athletes = await dbContext.Set<Participation>()
            .Where(p => p.Meet.PublishedResults && p.Meet.StartDate.Year == year)
            .Select(p => p.AthleteId)
            .Distinct()
            .CountAsync(cancellationToken);

        int records = await dbContext.Set<Record>()
            .Where(r => r.AttemptId != null && r.Date.Year == year)
            .CountAsync(cancellationToken);

        int clubs = await dbContext.Set<Participation>()
            .Where(p => p.Meet.PublishedResults && p.Meet.StartDate.Year == year && p.TeamId != null)
            .Select(p => p.TeamId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new DashboardSeasonStats(meets, athletes, records, clubs);
    }

    private async Task<List<DashboardRankingEntry>> GetTopRankingsAsync(
        string gender, int year, CancellationToken cancellationToken)
    {
        List<RawRankingRow> rows = await dbContext.Set<Participation>()
            .Where(p => !p.Disqualified)
            .Where(p => p.Athlete.Country.Iso2 == "IS")
            .Where(p => p.Meet.IsRaw)
            .Where(p => p.Meet.MeetType.MeetTypeId == 1)
            .Where(p => p.Meet.StartDate.Year == year)
            .Where(p => p.Total > 0)
            .Where(p => p.Athlete.Gender.Value == gender)
            .Select(p => new RawRankingRow(
                p.AthleteId,
                p.Athlete.Firstname + " " + p.Athlete.Lastname,
                p.Athlete.Slug,
                p.Total,
                p.WeightCategory.Title,
                p.Weight))
            .ToListAsync(cancellationToken);

        Gender parsedGender = Gender.Parse(gender);

        return rows
            .Select(r => new
            {
                r,
                IpfPoints = IpfPoints.Create(true, parsedGender, "Powerlifting", r.BodyWeight, r.Total).Value,
            })
            .GroupBy(x => x.r.AthleteId)
            .Select(g => g.OrderByDescending(x => x.IpfPoints).First())
            .OrderByDescending(x => x.IpfPoints)
            .Take(3)
            .Select(x => new DashboardRankingEntry(
                x.r.AthleteSlug,
                x.r.AthleteName,
                x.r.WeightCategory,
                x.IpfPoints))
            .ToList();
    }

    private async Task<List<DashboardRecordEntry>> GetRecentRecordsAsync(
        string gender, CancellationToken cancellationToken)
    {
        List<RawRecordRow> rows = await dbContext.Set<Record>()
            .Where(r => r.AttemptId != null)
            .Where(r => r.RecordCategoryId != RecordCategory.TotalWilks
                     && r.RecordCategoryId != RecordCategory.TotalIpfPoints)
            .Where(r => r.Attempt!.Participation.Athlete.Gender.Value == gender)
            .OrderByDescending(r => r.Date)
            .Take(3)
            .Select(r => new RawRecordRow(
                r.RecordCategoryId,
                r.Attempt!.Participation.Athlete.Slug,
                r.Attempt.Participation.Athlete.Firstname + " " + r.Attempt.Participation.Athlete.Lastname,
                r.WeightCategory.Title,
                r.AgeCategory.Slug,
                r.IsRaw,
                r.Weight,
                r.Attempt.Participation.Meet.Slug,
                r.Date))
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DashboardRecordEntry(
            r.RecordCategoryId.ToDisplayName(),
            r.AthleteSlug,
            r.AthleteName,
            r.WeightCategory,
            r.AgeCategory,
            r.IsRaw,
            r.Weight,
            r.MeetSlug,
            r.Date))
            .ToList();
    }

    private async Task<(List<DashboardTeamEntry> Men, List<DashboardTeamEntry> Women)> GetTeamStandingsAsync(
        int year, CancellationToken cancellationToken)
    {
        int bestN = TeamStandingsBuilder.GetBestN(year);

        List<TeamStandingsBuilder.TeamPointRow> rows = await dbContext.Set<Participation>()
            .Where(p => !p.Disqualified)
            .Where(p => p.Meet.IsInTeamCompetition)
            .Where(p => p.Meet.StartDate.Year == year)
            .Where(p => p.TeamId != null)
            .Where(p => p.TeamPoints != null && p.TeamPoints > 0)
            .Select(p => new TeamStandingsBuilder.TeamPointRow(
                p.TeamId!.Value,
                p.Team!.Title,
                p.Team.TitleShort,
                p.Team.Slug,
                p.Team.LogoImageFilename,
                p.Athlete.Gender.Value,
                p.MeetId,
                p.TeamPoints!.Value))
            .ToListAsync(cancellationToken);

        List<DashboardTeamEntry> men = TeamStandingsBuilder
            .BuildStandings(rows.Where(r => r.Gender == "m"), bestN)
            .Take(3)
            .Select(s => new DashboardTeamEntry(s.TeamSlug ?? string.Empty, s.TeamName, s.TotalPoints))
            .ToList();

        List<DashboardTeamEntry> women = TeamStandingsBuilder
            .BuildStandings(rows.Where(r => r.Gender == "f"), bestN)
            .Take(3)
            .Select(s => new DashboardTeamEntry(s.TeamSlug ?? string.Empty, s.TeamName, s.TotalPoints))
            .ToList();

        return (men, women);
    }

    private sealed record RawRankingRow(
        int AthleteId,
        string AthleteName,
        string AthleteSlug,
        decimal Total,
        string WeightCategory,
        decimal BodyWeight);

    private sealed record RawRecordRow(
        RecordCategory RecordCategoryId,
        string AthleteSlug,
        string AthleteName,
        string WeightCategory,
        string AgeCategory,
        bool IsRaw,
        decimal Weight,
        string MeetSlug,
        DateOnly Date);
}
```

- [ ] **Step 2: Create the endpoint**

```csharp
using KRAFT.Results.Contracts.Dashboard;

using Microsoft.AspNetCore.Http.HttpResults;

namespace KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

internal static class GetDashboardEndpoint
{
    internal static void MapGetDashboardEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard", HandleAsync)
            .WithName("GetDashboard")
            .WithTags("Dashboard")
            .AllowAnonymous();
    }

    private static async Task<Ok<DashboardSummary>> HandleAsync(
        GetDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        DashboardSummary summary = await handler.Handle(cancellationToken);
        return TypedResults.Ok(summary);
    }
}
```

- [ ] **Step 3: Create DashboardEndpoints.cs**

```csharp
using KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

namespace KRAFT.Results.WebApi.Features.Dashboard;

internal static class DashboardEndpoints
{
    internal static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetDashboardEndpoint();
        return endpoints;
    }
}
```

- [ ] **Step 4: Create DashboardServices.cs**

```csharp
using KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

namespace KRAFT.Results.WebApi.Features.Dashboard;

internal static class DashboardServices
{
    internal static IServiceCollection AddDashboard(this IServiceCollection services)
    {
        services.AddScoped<GetDashboardHandler>();
        return services;
    }
}
```

- [ ] **Step 5: Register in FeatureServices.cs**

Add to the existing `AddFeatures` method (add one line alongside the existing `services.Add*()` calls):

```csharp
// Add at top of file:
using KRAFT.Results.WebApi.Features.Dashboard;

// Inside AddFeatures():
services.AddDashboard();
```

- [ ] **Step 6: Register in FeatureEndpoints.cs**

Add to the existing `MapFeatureEndpoints` method:

```csharp
// Add at top of file:
using KRAFT.Results.WebApi.Features.Dashboard;

// Inside MapFeatureEndpoints():
builder.MapDashboardEndpoints();
```

- [ ] **Step 7: Build to verify no compile errors**

Run: `dotnet build src/KRAFT.Results.WebApi`  
Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~GetDashboardTests"`  
Expected: All 3 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add src/KRAFT.Results.WebApi/Features/Dashboard/ src/KRAFT.Results.WebApi/Features/FeatureServices.cs src/KRAFT.Results.WebApi/Features/FeatureEndpoints.cs
git commit -m "feat(dashboard): add GET /dashboard endpoint"
```

---

## Task 4: Frontend — DashboardPage component

**Files:**
- Create: `src/KRAFT.Results.Web.Client/Features/Dashboard/DashboardPage.razor`
- Create: `src/KRAFT.Results.Web.Client/Features/Dashboard/DashboardPage.razor.css`
- Delete: `src/KRAFT.Results.Web/Components/Pages/Home.razor`
- Delete: `src/KRAFT.Results.Web/Components/Pages/Home.razor.css`

- [ ] **Step 1: Delete the old empty home page**

```bash
git rm src/KRAFT.Results.Web/Components/Pages/Home.razor
git rm src/KRAFT.Results.Web/Components/Pages/Home.razor.css
```

- [ ] **Step 2: Create DashboardPage.razor**

```razor
@page "/"

@using KRAFT.Results.Contracts
@using KRAFT.Results.Contracts.Dashboard
@using KRAFT.Results.Contracts.Meets
@using KRAFT.Results.Web.Client.Components

@inject HttpClient HttpClient

<PageTitle>Forsíða — KRAFT Results</PageTitle>

@if (_isLoading)
{
    <LoadingSpinner Label="Sæki forsíðu..." />
}
else if (_hasError)
{
    <ErrorMessage Message="Villa kom upp við að sækja gögn. Reyndu aftur síðar." />
}
else if (_dashboard is not null)
{
    <h1>Forsíða</h1>

    <section class="season-stats" aria-label="Tölur ársins @_year">
        <h2 class="section-label">Tölur ársins @_year</h2>
        <div class="stat-bar">
            <div class="stat-card">
                <span class="stat-number">@_dashboard.SeasonStats.Meets</span>
                <span class="stat-label">Mót með niðurstöður</span>
            </div>
            <div class="stat-card">
                <span class="stat-number">@_dashboard.SeasonStats.Athletes</span>
                <span class="stat-label">Keppendur</span>
            </div>
            <div class="stat-card">
                <span class="stat-number">@_dashboard.SeasonStats.Records</span>
                <span class="stat-label">Met sett</span>
            </div>
            <div class="stat-card">
                <span class="stat-number">@_dashboard.SeasonStats.Clubs</span>
                <span class="stat-label">Félög</span>
            </div>
        </div>
    </section>

    <div class="widget">
        <div class="widget-header">
            <h2 class="widget-title">Mót</h2>
            <NavLink class="widget-link" href="/meets">Mótaskrá →</NavLink>
        </div>
        <div class="split-columns">
            <div class="split-col">
                <div class="split-col-header">Síðustu mót</div>
                @foreach (MeetSummary meet in _dashboard.RecentMeets)
                {
                    <article class="meet-item">
                        <div class="date-box date-box--recent">@meet.StartDate.Day.ToString("D2")</div>
                        <div>
                            <NavLink class="meet-title" href="@($"/meets/{meet.Slug}")">@meet.Title</NavLink>
                            <div class="meet-meta">@meet.StartDate.ToString("MMM", System.Globalization.CultureInfo.CurrentCulture) · @meet.Location</div>
                        </div>
                    </article>
                }
            </div>
            <div class="split-col">
                <div class="split-col-header">Næstu mót</div>
                @if (_dashboard.UpcomingMeets.Count == 0)
                {
                    <p class="empty-col">Engin mót skráð í dagatal.</p>
                }
                else
                {
                    @foreach (MeetSummary meet in _dashboard.UpcomingMeets)
                    {
                        <article class="meet-item">
                            <div class="date-box date-box--upcoming">@meet.StartDate.Day.ToString("D2")</div>
                            <div>
                                <NavLink class="meet-title" href="@($"/meets/{meet.Slug}")">@meet.Title</NavLink>
                                <div class="meet-meta">@meet.StartDate.ToString("MMM", System.Globalization.CultureInfo.CurrentCulture) · @meet.Location</div>
                            </div>
                        </article>
                    }
                }
            </div>
        </div>
    </div>

    <div class="widget">
        <div class="widget-header">
            <h2 class="widget-title">Stigatafla @_year — Klassík</h2>
            <NavLink class="widget-link" href="/rankings">Allt →</NavLink>
        </div>
        <div class="split-columns">
            <div class="split-col">
                <div class="split-col-header">Karlar</div>
                @foreach ((DashboardRankingEntry entry, int index) in _dashboard.TopRankingsMen.Select((e, i) => (e, i)))
                {
                    <div class="ranking-item">
                        <span class="rank-num rank-num--@RankClass(index + 1)">@(index + 1)</span>
                        <div class="rank-info">
                            <NavLink class="rank-athlete" href="@($"/athletes/{entry.AthleteSlug}")">@entry.AthleteName</NavLink>
                            <div class="rank-cat">@entry.WeightCategory</div>
                        </div>
                        <span class="rank-score">@entry.IpfPoints.ToString("F2")</span>
                    </div>
                }
            </div>
            <div class="split-col">
                <div class="split-col-header">Konur</div>
                @foreach ((DashboardRankingEntry entry, int index) in _dashboard.TopRankingsWomen.Select((e, i) => (e, i)))
                {
                    <div class="ranking-item">
                        <span class="rank-num rank-num--@RankClass(index + 1)">@(index + 1)</span>
                        <div class="rank-info">
                            <NavLink class="rank-athlete" href="@($"/athletes/{entry.AthleteSlug}")">@entry.AthleteName</NavLink>
                            <div class="rank-cat">@entry.WeightCategory</div>
                        </div>
                        <span class="rank-score">@entry.IpfPoints.ToString("F2")</span>
                    </div>
                }
            </div>
        </div>
    </div>

    <div class="widget">
        <div class="widget-header">
            <h2 class="widget-title">Ný met</h2>
            <NavLink class="widget-link" href="/records">Allt →</NavLink>
        </div>
        <div class="split-columns">
            <div class="split-col">
                <div class="split-col-header">Karlar</div>
                @foreach (DashboardRecordEntry record in _dashboard.RecentRecordsMen)
                {
                    <div class="record-item">
                        <div class="record-left">
                            <div class="record-top">
                                <span class="record-lift">@record.Lift</span>
                                <NavLink class="record-athlete" href="@($"/athletes/{record.AthleteSlug}")">@record.AthleteName</NavLink>
                            </div>
                            <div class="record-meta">
                                <span class="badge">@record.WeightCategory</span>
                                <span class="badge">@record.AgeCategory.ToAgeCategoryLabel()</span>
                                <span class="badge badge--@(record.IsClassic ? "classic" : "equipped")">@DisplayNames.EquipmentType(record.IsClassic)</span>
                            </div>
                        </div>
                        <div class="record-right">
                            <NavLink class="record-weight" href="@($"/meets/{record.MeetSlug}")">@record.Weight kg</NavLink>
                            <span class="record-date">@record.MeetDate.ToString("MMM yyyy", System.Globalization.CultureInfo.CurrentCulture)</span>
                        </div>
                    </div>
                }
            </div>
            <div class="split-col">
                <div class="split-col-header">Konur</div>
                @foreach (DashboardRecordEntry record in _dashboard.RecentRecordsWomen)
                {
                    <div class="record-item">
                        <div class="record-left">
                            <div class="record-top">
                                <span class="record-lift">@record.Lift</span>
                                <NavLink class="record-athlete" href="@($"/athletes/{record.AthleteSlug}")">@record.AthleteName</NavLink>
                            </div>
                            <div class="record-meta">
                                <span class="badge">@record.WeightCategory</span>
                                <span class="badge">@record.AgeCategory.ToAgeCategoryLabel()</span>
                                <span class="badge badge--@(record.IsClassic ? "classic" : "equipped")">@DisplayNames.EquipmentType(record.IsClassic)</span>
                            </div>
                        </div>
                        <div class="record-right">
                            <NavLink class="record-weight" href="@($"/meets/{record.MeetSlug}")">@record.Weight kg</NavLink>
                            <span class="record-date">@record.MeetDate.ToString("MMM yyyy", System.Globalization.CultureInfo.CurrentCulture)</span>
                        </div>
                    </div>
                }
            </div>
        </div>
    </div>

    @if (_dashboard.TeamStandingsMen.Count > 0 || _dashboard.TeamStandingsWomen.Count > 0)
    {
        <div class="widget">
            <div class="widget-header">
                <h2 class="widget-title">Liðakeppni @_year</h2>
                <NavLink class="widget-link" href="/team-competition">Allt →</NavLink>
            </div>
            <div class="split-columns">
                <div class="split-col">
                    <div class="split-col-header">Karlar</div>
                    @foreach ((DashboardTeamEntry team, int index) in _dashboard.TeamStandingsMen.Select((t, i) => (t, i)))
                    {
                        <div class="team-item">
                            <span class="rank-num rank-num--@RankClass(index + 1)">@(index + 1)</span>
                            <NavLink class="team-name" href="@($"/teams/{team.TeamSlug}")">@team.TeamName</NavLink>
                            <span class="team-points">@team.Points stig</span>
                        </div>
                    }
                </div>
                <div class="split-col">
                    <div class="split-col-header">Konur</div>
                    @foreach ((DashboardTeamEntry team, int index) in _dashboard.TeamStandingsWomen.Select((t, i) => (t, i)))
                    {
                        <div class="team-item">
                            <span class="rank-num rank-num--@RankClass(index + 1)">@(index + 1)</span>
                            <NavLink class="team-name" href="@($"/teams/{team.TeamSlug}")">@team.TeamName</NavLink>
                            <span class="team-points">@team.Points stig</span>
                        </div>
                    }
                </div>
            </div>
        </div>
    }

    <nav class="quick-nav" aria-label="Flýtileiðir">
        <NavLink class="quick-nav-card" href="/athletes">
            <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z"/></svg>
            <span>Keppendur</span>
        </NavLink>
        <NavLink class="quick-nav-card" href="/records">
            <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M16.5 18.75h-9m9 0a3 3 0 0 1 3 3h-15a3 3 0 0 1 3-3m9 0v-3.375c0-.621-.503-1.125-1.125-1.125h-.871M7.5 18.75v-3.375c0-.621.504-1.125 1.125-1.125h.872m5.007 0H9.497m5.007 0a7.454 7.454 0 0 1-.982-3.172M9.497 14.25a7.454 7.454 0 0 0 .981-3.172M5.25 4.236c-.982.143-1.954.317-2.916.52A6.003 6.003 0 0 0 7.73 9.728M5.25 4.236V4.5c0 2.108.966 3.99 2.48 5.228M5.25 4.236V2.721C7.456 2.41 9.71 2.25 12 2.25c2.291 0 4.545.16 6.75.47v1.516M7.73 9.728a6.726 6.726 0 0 0 2.748 1.35m8.272-6.842V4.5c0 2.108-.966 3.99-2.48 5.228m2.48-5.492a46.32 46.32 0 0 1 2.916.52 6.003 6.003 0 0 1-5.395 4.972m0 0a6.726 6.726 0 0 1-2.749 1.35m0 0a6.772 6.772 0 0 1-3.044 0"/></svg>
            <span>Met</span>
        </NavLink>
        <NavLink class="quick-nav-card" href="/rankings">
            <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z"/></svg>
            <span>Stigatöflur</span>
        </NavLink>
        <NavLink class="quick-nav-card" href="/teams">
            <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M18 18.72a9.094 9.094 0 0 0 3.741-.479 3 3 0 0 0-4.682-2.72m.94 3.198.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0 1 12 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 0 1 6 18.719m12 0a5.971 5.971 0 0 0-.941-3.197m0 0A5.995 5.995 0 0 0 12 12.75a5.995 5.995 0 0 0-5.058 2.772m0 0a3 3 0 0 0-4.681 2.72 8.986 8.986 0 0 0 3.74.477m.94-3.197a5.971 5.971 0 0 0-.94 3.197M15 6.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm6 3a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Zm-13.5 0a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Z"/></svg>
            <span>Félög</span>
        </NavLink>
    </nav>
}

@code {
    private bool _isLoading = true;
    private bool _hasError;
    private DashboardSummary? _dashboard;
    private int _year = DateTime.Now.Year;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _dashboard = await HttpClient.GetFromJsonAsync<DashboardSummary>("/dashboard");
        }
        catch (HttpRequestException)
        {
            _hasError = true;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string RankClass(int rank) => rank switch
    {
        1 => "gold",
        2 => "silver",
        3 => "bronze",
        _ => "default",
    };
}
```

- [ ] **Step 3: Create DashboardPage.razor.css**

```css
/* Season stats */
.section-label {
    font-family: var(--font-heading);
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-muted);
    margin: 0 0 0.625rem;
}

.stat-bar {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.75rem;
    margin-bottom: 1.5rem;
}

.stat-card {
    background: var(--color-white);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    padding: 0.875rem 1rem;
    text-align: center;
    box-shadow: var(--shadow-sm);
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.stat-number {
    font-family: var(--font-heading);
    font-size: 1.625rem;
    font-weight: 700;
    color: var(--color-text);
    line-height: 1;
}

.stat-label {
    font-size: 0.75rem;
    color: var(--color-text-muted);
}

/* Shared widget */
.widget {
    background: var(--color-white);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-sm);
    overflow: hidden;
    margin-bottom: 1.25rem;
}

.widget-header {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--color-border);
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.widget-title {
    font-family: var(--font-heading);
    font-size: 0.8125rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-text-muted);
    margin: 0;
}

.widget-link {
    font-size: 0.8125rem;
    font-weight: 600;
    color: var(--color-primary);
}

/* Split columns */
.split-columns {
    display: grid;
    grid-template-columns: 1fr 1fr;
}

.split-col + .split-col {
    border-left: 1px solid var(--color-border);
}

.split-col-header {
    padding: 0.5rem 1rem;
    background: #f9fafb;
    border-bottom: 1px solid var(--color-border);
    font-family: var(--font-heading);
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-text-muted);
}

.empty-col {
    padding: 0.75rem 1rem;
    font-size: 0.875rem;
    color: var(--color-text-muted);
    margin: 0;
}

/* Meet items */
.meet-item {
    display: flex;
    align-items: flex-start;
    gap: 0.75rem;
    padding: 0.7rem 1rem;
    border-bottom: 1px solid var(--color-border);
}

.meet-item:last-child {
    border-bottom: none;
}

.date-box {
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 700;
    width: 2.25rem;
    height: 2.25rem;
    border-radius: var(--radius-sm);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    color: var(--color-white);
}

.date-box--recent {
    background: var(--color-primary);
}

.date-box--upcoming {
    background: var(--color-text);
}

.meet-title {
    font-weight: 600;
    font-size: 0.9375rem;
    color: var(--color-primary);
    line-height: 1.3;
    display: block;
}

.meet-meta {
    font-size: 0.8125rem;
    color: var(--color-text-muted);
    margin-top: 0.1rem;
}

/* Rankings */
.ranking-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.6rem 1rem;
    border-bottom: 1px solid var(--color-border);
}

.ranking-item:last-child {
    border-bottom: none;
}

.rank-num {
    font-family: var(--font-heading);
    font-weight: 700;
    font-size: 1rem;
    width: 1.25rem;
    flex-shrink: 0;
}

.rank-num--gold   { color: #b8860b; }
.rank-num--silver { color: #6b7280; }
.rank-num--bronze { color: #9a5b2a; }
.rank-num--default { color: var(--color-text-muted); }

.rank-info {
    flex: 1;
    min-width: 0;
}

.rank-athlete {
    font-size: 0.875rem;
    font-weight: 600;
    display: block;
    color: var(--color-primary);
}

.rank-cat {
    font-size: 0.75rem;
    color: var(--color-text-muted);
}

.rank-score {
    font-family: var(--font-heading);
    font-size: 0.9375rem;
    font-weight: 700;
    color: var(--color-primary);
    flex-shrink: 0;
}

/* Records */
.record-item {
    display: flex;
    align-items: flex-start;
    gap: 0.625rem;
    padding: 0.6rem 1rem;
    border-bottom: 1px solid var(--color-border);
}

.record-item:last-child {
    border-bottom: none;
}

.record-left {
    flex: 1;
    min-width: 0;
}

.record-top {
    display: flex;
    align-items: baseline;
    gap: 0.375rem;
    flex-wrap: wrap;
}

.record-lift {
    font-size: 0.75rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--color-text-muted);
    flex-shrink: 0;
}

.record-athlete {
    font-size: 0.9rem;
    font-weight: 600;
    color: var(--color-primary);
}

.record-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem;
    margin-top: 0.25rem;
}

.badge {
    display: inline-block;
    font-size: 0.6875rem;
    font-weight: 600;
    padding: 0.1rem 0.375rem;
    border-radius: 3px;
    background: #f3f4f6;
    color: var(--color-text-muted);
    line-height: 1.4;
}

.badge--classic  { background: #eff6ff; color: #2563eb; }
.badge--equipped { background: #fef3c7; color: #d97706; }

.record-right {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    flex-shrink: 0;
}

.record-weight {
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 700;
    color: var(--color-primary);
}

.record-date {
    font-size: 0.75rem;
    color: var(--color-text-muted);
}

/* Team competition */
.team-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.6rem 1rem;
    border-bottom: 1px solid var(--color-border);
}

.team-item:last-child {
    border-bottom: none;
}

.team-name {
    flex: 1;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-primary);
}

.team-points {
    font-family: var(--font-heading);
    font-size: 0.9375rem;
    font-weight: 700;
    color: var(--color-primary);
}

/* Quick nav */
.quick-nav {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.75rem;
    margin-bottom: 1.25rem;
}

.quick-nav-card {
    background: var(--color-white);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-sm);
    padding: 1rem;
    text-align: center;
    color: var(--color-text);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.375rem;
    transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
}

.quick-nav-card:hover {
    border-color: var(--color-primary);
    box-shadow: var(--shadow-md);
}

.quick-nav-card svg {
    color: var(--color-primary);
}

.quick-nav-card span {
    font-family: var(--font-heading);
    font-size: 0.875rem;
    font-weight: 600;
}

/* ── Responsive ── */
@media (max-width: 640px) {
    .stat-bar {
        grid-template-columns: repeat(2, 1fr);
    }

    .split-columns {
        grid-template-columns: 1fr;
    }

    .split-col + .split-col {
        border-left: none;
        border-top: 1px solid var(--color-border);
    }

    .quick-nav {
        grid-template-columns: repeat(2, 1fr);
    }
}
```

- [ ] **Step 4: Build to verify no compile errors**

Run: `dotnet build src/KRAFT.Results.Web.Client`  
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Run all tests**

Run: `dotnet test`  
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/KRAFT.Results.Web.Client/Features/Dashboard/ src/KRAFT.Results.Web/Components/Pages/
git commit -m "feat(dashboard): add DashboardPage component"
```

---

## Task 5: Smoke test in browser

- [ ] **Step 1: Start the app**

Run: `dotnet run --project src/KRAFT.Results.AppHost`  
Wait for Aspire dashboard to open (usually `http://localhost:15057`).

- [ ] **Step 2: Verify the home page loads**

Open the web app URL shown in the Aspire dashboard. Confirm:
- Stat bar shows 4 numbers
- Meets widget shows two columns (Síðustu / Næstu)
- Rankings, Records, Team competition widgets appear
- Quick-nav row of 4 cards at the bottom
- No console errors

- [ ] **Step 3: Verify mobile layout**

Resize browser to < 640px. Confirm:
- Stat bar goes to 2 columns
- Split cards stack to a single column
- Quick-nav goes to 2 columns

- [ ] **Step 4: Final commit (if any fixes were needed during smoke test)**

```bash
git add -p
git commit -m "fix(dashboard): <describe any fix>"
```

---

## Self-Review

**Spec coverage check:**

| Spec requirement | Covered by |
|---|---|
| `GET /dashboard` anonymous endpoint | Task 3, GetDashboardEndpoint |
| `DashboardSummary` contract | Task 1 |
| Recent meets: last 3, `PublishedResults = true` | Task 3, handler |
| Upcoming meets: next 3, `PublishedInCalendar = true`, future date | Task 3, handler |
| Recent records (men/women): 3 each, with lift/weight cat/age cat/equipment/meet | Task 3 + Task 4 |
| Rankings top 3 (men/women): classic, current year, IPF points | Task 3, handler |
| Team standings (men/women): top 3, current year | Task 3, handler |
| Season stats: 4 current-year counts | Task 3, handler |
| Full-width split cards (meets, rankings, records, teams) | Task 4 CSS |
| Team competition card hidden if no data | Task 4 razor (`@if` guard) |
| Upcoming meets empty state | Task 4 razor |
| Responsive: stat bar 2-col, split cards stack, quick-nav 2-col on mobile | Task 4 CSS |
| Integration test | Task 2 |
| Home.razor replaced | Task 4 (delete + new `@page "/"`) |

No gaps found.
