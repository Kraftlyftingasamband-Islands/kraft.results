# Shared UI Components — Design Spec

**Date:** 2026-04-02
**Scope:** Extract repeated markup/CSS patterns into four shared Blazor components

---

## Problem

Four markup patterns are duplicated across many files in `KRAFT.Results.Web.Client`:

| Pattern | Files affected |
|---|---|
| Error message (`role="alert"` div) | ~15 files |
| Empty state div | ~8 files |
| Page header (title + admin action button + inline SVG) | ~7 files |
| Submit button (disabled/aria-busy + loading label swap) | ~6 forms |

The corresponding CSS rules are duplicated in parallel across each file's `.razor.css`.

---

## Approach

**Option B: Components + CSS consolidation.**
Create 4 new Blazor components. Each component owns its CSS in a single `.razor.css` file. Remove the duplicate CSS rules from all call sites. Not in scope: card base classes, form field components, hardcoded color variables.

---

## Components

### `ErrorMessage`

**File:** `Components/ErrorMessage.razor`

**Parameters:**

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Message` | `string` | Yes | — | The error text to display |
| `OnRetry` | `EventCallback?` | No | — | If bound, shows a retry button |

**Renders:**
```razor
<div role="alert" class="error-message">
    <p>@Message</p>
    @if (OnRetry.HasDelegate)
    {
        <button class="retry-btn" @onclick="OnRetry">Reyna aftur</button>
    }
</div>
```

**Usage:**
```razor
@if (_errorMessage is not null)
{
    <ErrorMessage Message="@_errorMessage" />
}

<ErrorMessage Message="Villa kom upp." OnRetry="LoadDataAsync" />
```

The null guard stays at the call site — `ErrorMessage` does not suppress itself when `Message` is null. Pages that use `_hasError` (bool) instead of a nullable string keep their existing `@if (_hasError)` guard.

**CSS:** Owns `.error-message` and `.retry-btn`. Unifies the `error-message` and `error-state` class names — `error-state` is dropped.

---

### `EmptyState`

**File:** `Components/EmptyState.razor`

**Parameters:**

| Parameter | Type | Required | Default |
|---|---|---|---|
| `Message` | `string` | Yes | — |

**Renders:**
```razor
<div class="empty-state">@Message</div>
```

**Usage:**
```razor
<EmptyState Message="Engin met fundust." />
```

**CSS:** Owns `.empty-state`.

---

### `PageHeader`

**File:** `Components/PageHeader.razor`

**Parameters:**

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Title` | `string?` | No | — | Renders as `<h3>`. Ignored if `TitleContent` is set. |
| `TitleContent` | `RenderFragment?` | No | — | Overrides `Title` for complex left-side content (e.g. `<YearNav>`) |
| `ActionHref` | `string?` | No | — | If null, no action button is rendered |
| `ActionLabel` | `string?` | No | — | Button text. Required when `ActionHref` is set. |
| `ActionRole` | `string` | No | `"Admin"` | Role for `<AuthorizeView>` gate |

**Renders:**
```razor
<div class="page-header">
    @if (TitleContent is not null)
    {
        @TitleContent
    }
    else if (Title is not null)
    {
        <h3>@Title</h3>
    }

    @if (ActionHref is not null)
    {
        <AuthorizeView Roles="@ActionRole">
            <Authorized>
                <a href="@ActionHref" class="btn-action">
                    <svg class="btn-icon" ...plus icon... aria-hidden="true" />
                    @ActionLabel
                </a>
            </Authorized>
        </AuthorizeView>
    }
</div>
```

**Usage:**
```razor
<!-- Common case: string title + admin button -->
<PageHeader Title="Keppendur" ActionHref="/athletes/create" ActionLabel="Stofna keppanda" />

<!-- YearNav as left content -->
<PageHeader ActionHref="/meets/create" ActionLabel="Nýtt mót">
    <TitleContent>
        <YearNav Label="Mótaskrá" Year="@Year" BaseUrl="/meets" />
    </TitleContent>
</PageHeader>

<!-- No action button -->
<PageHeader Title="Saga metsins" />
```

**CSS:** Owns `.page-header`, `.btn-action`, `.btn-icon`.

---

### `SubmitButton`

**File:** `Components/SubmitButton.razor`

**Parameters:**

| Parameter | Type | Required | Default |
|---|---|---|---|
| `Label` | `string` | Yes | — |
| `IsLoading` | `bool` | No | `false` |
| `LoadingLabel` | `string` | No | `"Augnablik..."` |

**Renders:**
```razor
<button type="submit" class="btn-primary" disabled="@IsLoading" aria-busy="@IsLoading">
    @if (IsLoading)
    {
        <span>@LoadingLabel</span>
    }
    else
    {
        <span>@Label</span>
    }
</button>
```

**Usage:**
```razor
<SubmitButton Label="Vista" IsLoading="@_isLoading" />
<SubmitButton Label="Stofna" IsLoading="@_isLoading" LoadingLabel="Stofna..." />
```

**CSS:** Owns `.btn-primary`. Removed from Login, CreateUserPage, MeetForm, TeamForm, AthleteForm, EditUserPage.

---

## CSS Cleanup

For each component introduced above, remove the corresponding CSS rules from all call-site `.razor.css` files. If removing those rules leaves a `.razor.css` file empty, delete the file.

Files to clean (partial list):

**ErrorMessage/RetryButton CSS** — remove from:
- `Login.razor.css`, `CreateUserPage.razor.css`, `MeetForm.razor.css`, `TeamForm.razor.css`, `AthleteForm.razor.css`, `EditUserPage.razor.css`
- `MeetIndex.razor.css`, `TeamsIndex.razor.css`, `AthletesIndex.razor.css`
- `AthleteDetailsPage.razor.css`, `MeetDetailsPage.razor.css`, `TeamDetailsPage.razor.css`
- `RecordsPage.razor.css`, `RankingsIndex.razor.css`, `RecordHistoryPage.razor.css`, `TeamCompetitionIndex.razor.css`
- `ConfirmDialog.razor.css`, `UserIndex.razor.css`, `EditMeetPage.razor.css`

**EmptyState CSS** — remove from:
- `RecordsPage.razor.css`, `RankingsIndex.razor.css`, `RecordHistoryPage.razor.css`
- `TeamCompetitionIndex.razor.css`, `MeetIndex.razor.css`, `AthletesIndex.razor.css`

**PageHeader/btn-action CSS** — remove from:
- `AthletesIndex.razor.css`, `TeamsIndex.razor.css`, `MeetIndex.razor.css`
- `AthleteDetailsPage.razor.css`, `MeetDetailsPage.razor.css`, `TeamDetailsPage.razor.css`
- `UserIndex.razor.css`

**SubmitButton/btn-primary CSS** — remove from:
- `Login.razor.css`, `CreateUserPage.razor.css`, `MeetForm.razor.css`
- `TeamForm.razor.css`, `AthleteForm.razor.css`, `EditUserPage.razor.css`

---

## Out of Scope

- Card base class / unifying `rc-*`, `r-*`, `p-*` etc. prefixes
- `<FormField>` component
- Hardcoded color values → CSS variables
- Font-size / spacing scale
- Inconsistent breakpoints

---

## File Locations

All new components go in `src/KRAFT.Results.Web.Client/Components/`.

No new contracts, API changes, or backend work required.
