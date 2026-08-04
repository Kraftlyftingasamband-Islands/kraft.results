# 0002. Age-category translation is a single server-side boundary

## Context

Age-category labels (`Öldungaflokkur 3`) were translated inconsistently — server-side in some handlers, client-side in some components, in a duplicated helper in one, and nowhere in two paths — causing raw English slugs/Titles to leak into pills. `ToAgeCategoryLabel` cannot be translated to SQL.

## Decision

Handlers translate the age-category slug (plus athlete gender) to the finished Icelandic label in memory after query materialization. DTO display fields named `AgeCategory` always carry the finished label; slug-bearing fields are named `*Slug`. Components render the field verbatim. Legitimate client-side `ToAgeCategoryLabel` calls survive only where the input is a genuine slug (route params, form option values, client-computed slugs, static page structure).

## Consequences

Easier: one place to change translation; no split-brain; pills cannot regress to English while the DTO carries the label. Harder: the label-vs-slug field convention is enforced by naming discipline, not the type system — a future value-object refactor (deferred) would make it compile-time safe.
