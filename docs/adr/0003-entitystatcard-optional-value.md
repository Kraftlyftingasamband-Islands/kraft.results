# EntityStatCard Optional Value

## Context

Ghost meets (upcoming meets with no participants) must render no stat column for non-admin users, while every other entity card always shows a participant count or similar value. The `EntityStatCard` component previously required `Value` via `[EditorRequired]` and `required string`.

## Decision

`Value` is changed to `string?` and `[EditorRequired]` is dropped. The `.esc-stat` column is suppressed entirely when both `Label` and `Value` are null or empty. A `Clickable` bool parameter is also added, forwarded to the underlying `Card` component.

## Consequences

Loses the compile-time nudge that every card must show a value. Accepted because the stat column is genuinely optional for ghost meets, and the alternative — a separate no-stat card variant — would duplicate the entity-card layout that the migration to `EntityStatCard` is intended to unify.
