# DOMAIN.md

Domain concepts for KRAFT.Results — a powerlifting competition results management system for the Icelandic Powerlifting Federation (KRAFT). This document covers concepts that are non-obvious or have implementation trade-offs worth recording.

---

## Bans and Disqualification

### Ban

A **Ban** is a period during which an athlete is prohibited from competing. It is defined by `FromDate` and `ToDate`, both stored as `DateTime`. Comparisons are made at **date granularity** using `DateOnly.FromDateTime()`.

### Active ban

A ban is **active** on a given date when that date falls within `[FromDate, ToDate]` inclusive:

```csharp
// Athlete.cs
internal bool HasActiveBan(DateOnly date)
{
    return Bans.Any(ban =>
        date >= DateOnly.FromDateTime(ban.FromDate)
        && date <= DateOnly.FromDateTime(ban.ToDate));
}
```

`IsEligibleForRecord(DateOnly meetDate)` delegates to `!HasActiveBan(meetDate)`.

### Meet date used for ban checks

`Meet.StartDate` is used as the reference date when evaluating whether an athlete has an active ban. This is a pragmatic approximation (see WADA note below). The field is stored as `DateTime`; the ban check converts it via `DateOnly.FromDateTime(Meet.StartDate)`.

### Disqualification derivation

`Participation.Disqualified` is **derived**, never set manually. `RecalculateTotals()` computes it as:

```
Disqualified = bombedOut || HasActiveBan(meetDate)
```

where:

- `bombedOut` — the athlete has zero good lifts in any required discipline (Squat, Bench, or Deadlift)
- `HasActiveBan(meetDate)` — the athlete has at least one ban whose range covers `Meet.StartDate`

`RecalculateTotals()` requires both `Participation.Meet` and `Participation.Athlete` (with `Athlete.Bans`) to be loaded; it throws `InvalidOperationException` if either navigation property is null.

### Preserved values for banned athletes

Banned athletes keep their computed `Total`, `Wilks`, and `IpfPoints` values. Only `Disqualified` is set to `true`. This distinguishes the two disqualification causes:

| Condition | Disqualified | Total |
|---|---|---|
| Bomb-out | `true` | `0` |
| Active ban | `true` | `> 0` |

### Downstream effects

All views — rankings, personal bests, team points, meet display — filter on `Disqualified`. Setting the flag at the source (in `RecalculateTotals`) propagates correctness everywhere without additional special-casing.

### Retroactive cascade

When a ban is added or removed, a domain event is raised on the `Athlete` aggregate root:

- `BanAddedEvent` — raised by `Athlete.AddBan()`
- `BanRemovedEvent` — raised when a ban is removed (future: `Athlete.RemoveBan()`)

`BanEventHandler` handles both events and runs the retroactive cascade for all participations whose meet falls within the ban period:

1. `RecalculateTotals()` — updates `Disqualified` (and lifts/Total)
2. `PlaceComputationService.ComputePlacesAsync()` — recomputes meet placements, excluding disqualified athletes from ranked positions
3. `RecordComputationService.RebuildSlotsAsync()` — rebuilds record slots for affected age/weight/discipline combinations

The cascade runs with one `SaveChangesAsync` call per participation iteration (to persist `Disqualified` before place computation queries) plus a trailing `SaveChangesAsync` for place and rank updates.

---

## WADA / IPF Reference

KRAFT (Icelandic Powerlifting Federation) follows IPF rules, which implement the WADA Code. Anti-doping oversight for Icelandic athletes is handled by **Lyfjaeftirlit Íslands** (Icelandic Anti-Doping Authority). KRAFT does not maintain independent ban rules.

### Competition vs. Event under WADA

Under the WADA Code, a **Competition** is a single lifting session; an **Event** is the multi-day championship. Ban eligibility is evaluated per Competition (i.e., per lifting session), not per Event.

The system approximates this by using `Meet.StartDate` for all participants, regardless of which session they lifted in. Icelandic meets are typically 1–2 days, making this edge case negligible in practice. If a per-session date is ever added to `Participation`, the ban check becomes a one-line change in `RecalculateTotals()`.
