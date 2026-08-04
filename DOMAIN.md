# DOMAIN.md

Domain concepts for KRAFT.Results — a powerlifting competition results management system for the Icelandic Powerlifting Federation (KRAFT). This document covers concepts that are non-obvious or have implementation trade-offs worth recording.

---

## Club

### Club vs Team naming

**Club** is the correct domain term — athletes compete for clubs affiliated with KRAFT. The server entity is currently named `Team` (`Team.cs`), which is a naming misnomer; participation-context DTOs already use the domain term (`Club`, `ClubSlug`, `ClubLogoImageFilename` on `MeetParticipation`). New code should prefer **Club** in naming and UI copy.

### Club identity fields

A club has a full title and a **short name** (`TitleShort` on the entity). The short name is the display form used in dense contexts such as attempt cards on meet results.

### Club logo

A club's logo is **optional** — `LogoImageFilename` holds a blob filename resolved against the configured `ImageBaseUrl` (rendered by the `TeamLogo` component with size-specific resize/crop and 2x srcset). Display fallback rules:

- **Attempt cards** — logo when available, otherwise the club short name as text. Any unusable logo (missing, unsafe filename, or runtime 404) degrades to the short name.
- **Club-centric views** (clubs index, club details, team-competition standings) — logo shown beside the name; a shield-icon placeholder stands in when no logo exists.

---

## Meet Visibility

### PublishedResults vs. having results

`PublishedResults` is a **manually set flag** on the Meet — an editorial decision to make the meet's results page public. It defaults to `true` at meet creation and says nothing about whether result data exists. A meet can be published yet empty.

### Has actual results

A meet **has actual results** when at least one of its participations has at least one recorded attempt. This is a **data-existence check**, independent of `PublishedResults`. A meet where every lifter bombed out (all totals 0) still has actual results — the attempts happened.

The strictness ladder, for reference: has participants (registrations only) < has recorded attempts (**the chosen definition**) < has a valid total (`Total > 0`, the idiom used for individual-athlete result checks in rankings and records).

### Dashboard eligibility for past meets

The dashboard's recent-meets list shows only past meets (`StartDate <= today`) that are published (`PublishedResults`) **and** have actual results. The filter is role-agnostic — admins see the same list; empty past meets remain reachable via the meets index, which deliberately lists all meets.

---

## Age Categories

### Age-category slug

The **slug** is the canonical machine code for an age category: `open`, `subjunior`, `junior`, `masters1`–`masters4`. It is the value stored on the `AgeCategory` reference row (7 fixed rows), used in routes (`/records/{gender}/{ageCategory}`), form option values, and API filters. The DB `Title` column ("Open", "Masters 3") is an English legacy label inherited from the old system and is never shown in the UI.

### Icelandic age-category label

The user-facing name, produced solely by `DisplayNames.ToAgeCategoryLabel(slug, gender)` in Contracts: `open` → "Opinn flokkur", `junior` → "Unglingaflokkur", `masters1`–`masters4` → "Öldungaflokkur 1–4", and `subjunior` → "Drengjaflokkur" (male) / "Stúlknaflokkur" (female) — the only gender-dependent label, so gender must always be passed when the slug can be `subjunior`. Unknown input yields `string.Empty`, not the input. The Icelandic names are an invention of this codebase; the old system displayed English titles.

### Translation boundary convention

Age-category **display fields in DTOs carry the finished Icelandic label, translated server-side in the handler** (from slug + gender, falling back to the raw `Title` when translation yields empty). The client renders DTO display fields verbatim. Client-side calls to `ToAgeCategoryLabel` are reserved for slug-as-data cases where no DTO display field exists: route parameters (`RecordsPage`), form option values (`AddParticipantForm`), client-computed slugs (`BirthYear`), and static page structure (`RecordsIndex`). Fields carrying a slug are named `*Slug` (e.g. `AgeCategorySlug`); a field named `AgeCategory` in a DTO is always the Icelandic label.

### Slug cascade

A record set in a more specific category can also count in broader ones. `AgeCategory.GetCascadeSlugs` expands a biological slug (from `AgeCategory.ResolveSlug(dateOfBirth, meetDate)`, per IPF age bands) into all categories the lift competes in; `RecordAgeCategorySelector.SelectBestLabel` picks the most specific non-`open` slug when labeling a record.

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

## Records

### Record slot

A **record slot** is one current-record position keyed by equipment era × discipline (record category) × age category × weight category. `IsCurrent` marks the row holding the slot; superseded rows are retained as history. The athlete details page shows only current-era slots (`Era.EndDate` in the future) the athlete currently holds.

### Record cascade

A **single successful attempt can set multiple records at once**. Two axes cascade independently:

- **Age categories** — the attempt counts in every age category the athlete qualifies for on the meet date, cascading toward the open category. A masters lifter cascades down through the younger masters bands (e.g. M4 → M3 → M2 → M1 → O); a junior lifter cascades up (e.g. sub-junior → junior → O).
- **Record type** — a lift can take both the full-meet (within-powerlifting) record and the single-lift record for that discipline.

Example ceiling: an M4 deadlift record cascading to open can be **8 different records** (O, M1, M2, M3, M4 across single-lift and full-meet types).

UI displaying "this result is a record" must therefore handle one result mapping to many record slots.

### Equipment era

Records are scoped by equipment: **classic** (raw) vs. **equipped**, and by era (`Era` date range). Only slots in the current era are "current records".

### Single-lift and discipline flags

- **Single-lift record** (`IsSingleLift`) — set in a single-lift meet (e.g. bench-only). Marked "SL" in the UI.
- **Within-powerlifting record** (`IsWithinPowerlifting`) — a bench/deadlift record achieved during a full powerlifting meet.
- **Standalone-discipline record** (`IsStandaloneDiscipline`) — the discipline is contested as its own event.

### Personal best vs. record

A **personal best** is the athlete's best result per equipment × discipline × single-lift combination. A personal best may simultaneously hold multiple current record slots (via the record cascade). Which slots a PB holds is server-derivable knowledge — the record rows stem from the same result — and is computed on the API, not re-derived client-side.

---

## WADA / IPF Reference

KRAFT (Icelandic Powerlifting Federation) follows IPF rules, which implement the WADA Code. Anti-doping oversight for Icelandic athletes is handled by **Lyfjaeftirlit Islands** (Icelandic Anti-Doping Authority). KRAFT does not maintain independent ban rules.

### Competition vs. Event under WADA

Under the WADA Code, a **Competition** is a single lifting session; an **Event** is the multi-day championship. Ban eligibility is evaluated per Competition (i.e., per lifting session), not per Event.

The system approximates this by using the meet's start date for all participants, regardless of which session they lifted in. Icelandic meets are typically 1-2 days, making this edge case negligible in practice. If a per-session date is ever added to participations, the ban check is a one-line change.
