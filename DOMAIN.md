# DOMAIN.md

Domain concepts for KRAFT.Results — a powerlifting competition results management system for the Icelandic Powerlifting Federation (KRAFT). This document covers concepts that are non-obvious or have implementation trade-offs worth recording.

---

## Bans and Disqualification

### Ban

A **Ban** is a period during which an athlete is prohibited from competing. It is defined by `FromDate` and `ToDate`. Comparisons are made at **date granularity** (time-of-day is ignored).

### Active ban

A ban is **active** on a given date when that date falls within `[FromDate, ToDate]` inclusive. Multiple bans may overlap — removing one does not clear the active status if another covers the same date.

### Meet date used for ban checks

The meet's start date is used as the reference date when evaluating whether an athlete has an active ban. This is a pragmatic approximation (see WADA note below).

### Disqualification derivation

A participation's disqualification status is **derived**, never set manually. It is recomputed whenever attempts are recorded. The formula is:

> Disqualified = bombed out OR has active ban on meet date

where:

- **Bombed out** — the athlete has zero good lifts in any required discipline (Squat, Bench, or Deadlift)
- **Has active ban** — the athlete has at least one ban whose date range covers the meet's start date

### Preserved values for banned athletes

Banned athletes keep their computed Total, Wilks, and IpfPoints values. Only the disqualification flag is set. This distinguishes the two disqualification causes:

| Condition | Disqualified | Total |
|---|---|---|
| Bomb-out | yes | 0 |
| Active ban | yes | > 0 |

### Downstream effects

All views — rankings, personal bests, team points, meet display — filter on the disqualification flag. Setting it at the source propagates correctness everywhere without additional special-casing.

### Retroactive cascade

When a ban is added or removed, a domain event is raised on the Athlete aggregate root. The event handler runs a retroactive cascade for all participations whose meet falls within the ban period:

1. Recompute totals and disqualification status
2. Recompute meet placements, excluding disqualified athletes from ranked positions
3. Rebuild record slots for affected age/weight/discipline combinations

---

## WADA / IPF Reference

KRAFT (Icelandic Powerlifting Federation) follows IPF rules, which implement the WADA Code. Anti-doping oversight for Icelandic athletes is handled by **Lyfjaeftirlit Islands** (Icelandic Anti-Doping Authority). KRAFT does not maintain independent ban rules.

### Competition vs. Event under WADA

Under the WADA Code, a **Competition** is a single lifting session; an **Event** is the multi-day championship. Ban eligibility is evaluated per Competition (i.e., per lifting session), not per Event.

The system approximates this by using the meet's start date for all participants, regardless of which session they lifted in. Icelandic meets are typically 1-2 days, making this edge case negligible in practice. If a per-session date is ever added to participations, the ban check is a one-line change.