# Dashboard (Home Page) Design

**Date:** 2026-04-04  
**Status:** Approved

## Overview

Replace the empty home page with a dashboard giving all three user types (general public, athletes, federation admins) a useful at-a-glance view of the current season.

## Data Fetching

Single `GET /dashboard` endpoint (anonymous). One round trip, one spinner. New `Features/Dashboard/` slice in `WebApi`.

```
Features/Dashboard/
├── GetDashboard/
│   ├── GetDashboardEndpoint.cs   GET /dashboard, anonymous
│   └── GetDashboardHandler.cs
└── DashboardServices.cs
```

New `DashboardSummary` sealed record in `Contracts/Dashboard/`.

### Handler queries (all in one DB call via projections)

| Data | Query |
|------|-------|
| Recent meets | Last 3 by `StartDate desc` where `PublishedResults = true` |
| Upcoming meets | Next 3 by `StartDate asc` where `StartDate > today` and `PublishedInCalendar = true` |
| Recent records (men) | 3 most recently set, gender = male, ordered by meet `StartDate desc` |
| Recent records (women) | 3 most recently set, gender = female, ordered by meet `StartDate desc` |
| Rankings top 3 (men) | Top 3 IPF points, classic, current year, gender = male |
| Rankings top 3 (women) | Top 3 IPF points, classic, current year, gender = female |
| Team standings (men) | Top 3 teams, current year, gender = male |
| Team standings (women) | Top 3 teams, current year, gender = female |
| Season stats | 4 counts for current year (see below) |

### Season stats (current year, `StartDate.Year == now.Year`)

- Meets with `PublishedResults = true`
- Distinct athletes with at least one participation in a published meet
- Records set (approved records whose meet falls in this year)
- Clubs with at least one athlete in a published meet

## Contract

```csharp
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

public sealed record DashboardSeasonStats(int Meets, int Athletes, int Records, int Clubs);

public sealed record DashboardRankingEntry(string AthleteSlug, string AthleteName, string WeightCategory, decimal IpfPoints);

public sealed record DashboardRecordEntry(
    string Lift,           // "squat" | "bench" | "deadlift" | "total"
    string AthleteSlug,
    string AthleteName,
    string WeightCategory,
    string AgeCategory,    // "open" | "subjunior" | "junior" | "masters1" etc.
    bool IsClassic,
    decimal Weight,
    string MeetSlug,
    DateOnly MeetDate
);

public sealed record DashboardTeamEntry(string TeamSlug, string TeamName, decimal Points);
```

## Frontend

New `Features/Dashboard/DashboardPage.razor` in `Web.Client`. Rendered by existing `Home.razor` (currently empty).

### Page layout (top to bottom)

1. **Tölur ársins {year}** — 4-column stat bar (meets, athletes, records, clubs)
2. **Mót** — full-width split card: left = Síðustu mót (3), right = Næstu mót (3)
3. **Stigatafla {year} — Klassík** — full-width split card: left = Karlar top 3, right = Konur top 3
4. **Ný met** — full-width split card: left = Karlar (3), right = Konur (3)
5. **Liðakeppni {year}** — full-width split card: left = Karlar top 3, right = Konur top 3
6. **Quick nav row** — 4 cards linking to Keppendur, Met, Stigatöflur, Félög

### Split card pattern (shared across widgets 2–5)

Each split card has:
- Widget header with title + "Allt →" link
- Two equal columns separated by a vertical divider
- Sub-column header ("Karlar" / "Konur", or "Síðustu mót" / "Næstu mót")

### Record item detail

Each record row shows:
- Lift type label (Hnébeyging / Bekkur / Teygsla / Samtals)
- Athlete name (links to athlete page)
- Badges: weight category · age category · equipment (Klassík = blue, Búnaður = amber)
- Weight lifted (right-aligned)
- Meet month + year (right-aligned, links to meet)

### Meet item detail

- Date box (day of month) — red for recent, dark for upcoming
- Meet title (links to meet)
- Month · Location

### Empty states

- No upcoming meets: show "Engin mót skráð í dagatal" in the upcoming column
- No team competition data: hide the team competition card entirely

## Responsive / Mobile

All split cards stack vertically on mobile: each column becomes full-width, sub-column header still shown, divider becomes a horizontal rule. Breakpoint: `< 640px`.

- Stat bar: 2×2 grid on mobile (2 columns instead of 4)
- Quick-nav row: 2×2 grid on mobile
- Split cards: single column, sub-headers preserved
- Record badge row wraps naturally — no truncation

## Scoped CSS

New `DashboardPage.razor.css`. Stat bar, split-card layout, record badges, quick-nav, and responsive breakpoints are dashboard-specific and should not go in `app.css`.

## Testing

Integration test: `GET /dashboard` returns 200 with valid `DashboardSummary`. Seed data must include at least one published meet with participations, one approved record, and one team competition entry to exercise all fields.
