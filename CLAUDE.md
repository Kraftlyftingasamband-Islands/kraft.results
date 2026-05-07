# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KRAFT.Results is a rewrite of [results.kraft.is](https://results.kraft.is) — a powerlifting competition results management system for the Icelandic Powerlifting Federation. The original source (ASP.NET MVC 5 / .NET Framework 4.6.2 / LINQ to SQL) is at `C:\Users\kar\Downloads\Results.Kraft.Is`. **Note:** The old codebase may not be up to date with what is running in production. The live site at results.kraft.is is the source of truth — our code should always produce the same values as the live site, given that the local database is an up-to-date copy of the production database. Use the old source for reference only; always verify against the live site.

## Commands

```bash
# Build
dotnet build

# Test (requires Docker for SQL Server Testcontainers)
dotnet test

# Run locally (starts SQL Server container, migrates DB, seeds data)
dotnet run --project src/KRAFT.Results.AppHost

# Run single test
dotnet test --filter "FullyQualifiedName~CreateAthleteTests.ReturnsCreated_WhenSuccessful"

# Add EF migration
dotnet ef migrations add {MigrationName} --project src/KRAFT.Results.WebApi
```

## Architecture

**.NET 10 / .NET Aspire** solution with these projects:

| Project | Role |
|---------|------|
| `AppHost` | Aspire orchestrator — SQL Server container, service wiring |
| `WebApi` | ASP.NET Core Minimal API backend |
| `Web` | Blazor Server host |
| `Web.Client` | Blazor WebAssembly components |
| `Contracts` | Shared DTOs (sealed records) between API and clients |
| `ServiceDefaults` | OpenTelemetry, health checks, resilience defaults |

### Feature-Sliced Design (WebApi)

Each domain feature is self-contained under `Features/{Feature}/`:

```
Features/Athletes/
├── Athlete.cs                  # Entity with static Create() factory
├── AthleteConfiguration.cs     # IEntityTypeConfiguration<T>
├── AthleteEndpoints.cs         # Route group registration
├── AthleteServices.cs          # DI extension method
├── AthleteErrors.cs            # Static error definitions
├── Create/
│   ├── CreateAthleteEndpoint.cs
│   └── CreateAthleteHandler.cs # Returns Result<T>
├── Get/
└── GetDetails/
```

Registration flows through `FeatureServices.cs` → `builder.Services.AddFeatures()` and `FeatureEndpoints.cs` → `app.MapFeatures()`.

### Key Patterns

- **Result pattern** (`Abstractions/Result.cs`): All handlers return `Result<T>`. Endpoints call `result.Match()` to map to HTTP responses.
- **Value objects** (`ValueObjects/`): `Gender`, `Slug`, `Email`, `Password`, `IpfPoints` — each with validation via `ValueObject<T>` base.
- **Entity factories**: Entities have private constructors and `internal static Result<T> Create(...)` methods.
- **Error definitions**: `{Entity}Errors` static classes with factory methods returning `Error` records.
- **Auditing**: Entities carry `CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`.

### Database

SQL Server 2022 via EF Core 10. Connection string name: `kraft-db`. Fluent API configuration only (no data annotations). To populate with production data, see `README.md`.

### Auth

JWT Bearer tokens. `TokenProvider` generates tokens, `IHttpContextService` extracts claims. Protected endpoints use `.RequireAuthorization()`. Tests use `TestAuthHandler` for mock auth.

## Testing

**xUnit 3 + Shouldly + Testcontainers.MsSql**. Integration tests only — they hit a real SQL Server container.

- `DatabaseFixture` — spins up SQL Server, runs migrations, seeds immutable infrastructure (countries, eras, weight/age categories, users/roles)
- `IntegrationTestFactory` — `WebApplicationFactory<Program>` with test DB
- `Builders/` — builder pattern for commands (e.g. `CreateAthleteCommandBuilder`)

### Test data seeding

Create test data through HTTP endpoints, not raw SQL. Tests should drive the real API workflow. SQL is acceptable only for fields that have no write endpoint yet (Bans) and for Place when the meet has `CalculatePlaces = false` (otherwise places are calculated in GET endpoints). TeamPoints is a computed field derived from meet placements — use the computation endpoint when available. When adding or updating an endpoint that makes a previously SQL-only field settable through the API, migrate any remaining test SQL for that field to use the endpoint, and update this section to reflect the current state (see #433).

#### Seeding recipe

1. `POST /athletes` — create athletes, track slugs for cleanup
2. `POST /meets` — create meets, extract slug from `Location` header, track slugs for cleanup
3. `GET /meets/{slug}` — read back `MeetId` (needed for participant/attempt endpoints)
4. `POST /meets/{meetId}/participants` — add participant, read `ParticipationId` from response, track `(MeetId, ParticipationId)` tuples for cleanup
5. `PUT /meets/{meetId}/participants/{participationId}/attempts/{discipline}/{round}` — record attempts (Squat=0, Bench=1, Deadlift=2)
6. `await _channel.WaitUntilDrainedAsync(...)` — wait for async record computation to finish after each participant's attempts

Total, Wilks, and IpfPoints are **computed side effects** of recording attempts — never set them via SQL.

#### Cleanup recipe (DisposeAsync)

Clean up in reverse FK order using endpoints:

1. `DELETE /meets/{meetId}/participants/{participationId}` — cascades to records, attempts, and participation
2. `DELETE /meets/{slug}` — delete meets
3. `DELETE /athletes/{slug}` — delete athletes

Use tracked lists (`_participations`, `_meetSlugs`, `_athleteSlugs`) populated during setup. No SQL cleanup needed.

#### Reference implementation

`GetRecordsTests.cs` is the canonical example of a fully migrated test — use it as the pattern for all remaining migrations.

## Code Style

Key `.editorconfig` rules (warnings are errors in build):
- **No `var`** — explicit types enforced (`csharp_style_var_*: error`)
- **Private fields**: `_camelCase`; static private: `s_camelCase`
- **File-scoped namespaces**
- **Braces required** on all blocks
- Centralized package versions in `Directory.Packages.props`
