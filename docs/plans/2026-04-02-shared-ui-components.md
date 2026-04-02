# Shared UI Components Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract four repeated markup patterns (ErrorMessage, EmptyState, PageHeader, SubmitButton) into shared Blazor components and move their CSS into `app.css`.

**Architecture:** New components live in `Components/` (auto-imported via `_Imports.razor`). CSS for all four patterns moves to `app.css` as global styles — not scoped — because several of the classes (e.g. `.btn-action`, `.error-message`) are also used in pages that keep their own custom markup and won't use the component.

**Tech Stack:** Blazor WebAssembly / Blazor Server, Razor components (`.razor`), CSS isolation, ASP.NET Core `AuthorizeView`

---

## File Map

**Create:**
- `src/KRAFT.Results.Web.Client/Components/ErrorMessage.razor`
- `src/KRAFT.Results.Web.Client/Components/EmptyState.razor`
- `src/KRAFT.Results.Web.Client/Components/PageHeader.razor`
- `src/KRAFT.Results.Web.Client/Components/SubmitButton.razor`

**Modify — add CSS:**
- `src/KRAFT.Results.Web\wwwroot/app.css`

**Modify — replace markup, remove duplicate CSS rules:**

*ErrorMessage call sites (non-retry):*
- `Features/Athletes/AthletesIndex.razor` + `.razor.css`
- `Features/Meets/MeetIndex.razor` + `.razor.css`
- `Features/Teams/TeamsIndex.razor` + `.razor.css`
- `Features/Users/UserIndex.razor` + `.razor.css`
- `Features/Auth/Login.razor` + `.razor.css`
- `Features/Users/CreateUserPage.razor` + `.razor.css`
- `Features/Users/EditUserPage.razor` + `.razor.css` *(form-level error only; page-load error has back-link, skip)*
- `Features/Athletes/AthleteForm.razor` + `.razor.css`
- `Features/Meets/MeetForm.razor` + `.razor.css`
- `Features/Teams/TeamForm.razor` + `.razor.css`

*ErrorMessage call sites (retry variant):*
- `Features/Records/RecordsPage.razor` + `.razor.css`
- `Features/Rankings/RankingsIndex.razor` + `.razor.css`
- `Features/Records/RecordHistoryPage.razor` + `.razor.css`
- `Features/TeamCompetition/TeamCompetitionIndex.razor` + `.razor.css`

*CSS-only cleanup (keep own markup — has back-links or complex action areas):*
- `Features/Athletes/AthleteDetailsPage.razor.css`
- `Features/Meets/MeetDetailsPage.razor.css`
- `Features/Teams/TeamDetailsPage.razor.css`
- `Features/Meets/EditMeetPage.razor.css`

*EmptyState call sites:*
- `Features/Records/RecordsPage.razor` (already modified above)
- `Features/Rankings/RankingsIndex.razor` (already modified above)
- `Features/Records/RecordHistoryPage.razor` (already modified above)
- `Features/TeamCompetition/TeamCompetitionIndex.razor` (already modified above)
- `Features/Athletes/AthletesIndex.razor` (already modified above)

*PageHeader call sites (index pages only):*
- `Features/Athletes/AthletesIndex.razor` (already modified above)
- `Features/Teams/TeamsIndex.razor` (already modified above)
- `Features/Meets/MeetIndex.razor` (already modified above)

*SubmitButton call sites:*
- `Features/Auth/Login.razor` (already modified above)
- `Features/Users/CreateUserPage.razor` (already modified above)
- `Features/Users/EditUserPage.razor` (already modified above)
- `Features/Athletes/AthleteForm.razor` (already modified above)
- `Features/Meets/MeetForm.razor` (already modified above)
- `Features/Teams/TeamForm.razor` (already modified above)

---

## Task 1: Add shared CSS to app.css

**Files:**
- Modify: `src/KRAFT.Results.Web/wwwroot/app.css`

- [ ] **Step 1.1: Append shared CSS rules to the end of app.css**

Add the following block after the existing `.blazor-error-boundary::after` rule:

```css
/* ===== Shared component styles ===== */

/* Error message (inline, red box) */
.error-message {
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}

/* Error state (centred, with retry button) */
.error-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    padding: 2rem 1rem;
    text-align: center;
    color: var(--color-text-muted);
    font-size: 0.9375rem;
}

.retry-btn {
    padding: 0.75rem 1.5rem;
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-body);
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-white);
    background-color: var(--color-primary);
    cursor: pointer;
    transition: background-color var(--transition-fast) ease;
}

.retry-btn:hover {
    background-color: var(--color-primary-dark);
}

/* Empty state */
.empty-state {
    padding: 2rem 1rem;
    text-align: center;
    color: var(--color-text-muted);
    font-size: 0.9375rem;
}

/* Page header */
.page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-block-end: 1.25rem;
}

.page-header h3 {
    margin: 0;
}

/* Action button (header create/edit links) */
.btn-action {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    padding: 0.5rem 1rem;
    background: var(--color-primary);
    color: var(--color-white);
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-body);
    font-size: 0.875rem;
    font-weight: 600;
    letter-spacing: 0.02em;
    cursor: pointer;
    text-decoration: none;
    transition: background-color var(--transition-fast) ease;
}

.btn-icon {
    width: 1.125rem;
    height: 1.125rem;
    flex-shrink: 0;
}

.btn-action:hover {
    background: var(--color-primary-dark);
}

.btn-action:focus-visible {
    outline-color: var(--color-text);
}

/* Primary submit button */
.btn-primary {
    width: 100%;
    padding: 0.875rem 1.5rem;
    background: var(--color-primary);
    color: var(--color-white);
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 600;
    letter-spacing: 0.02em;
    cursor: pointer;
    transition: background-color var(--transition-fast) ease;
}

.btn-primary:hover {
    background: var(--color-primary-dark);
}

.btn-primary:focus-visible {
    outline-color: var(--color-text);
}

.btn-primary:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}
```

Note: The existing `@media (prefers-reduced-motion)` rule in `app.css` already sets `transition-duration: 0.01ms !important` on all elements, so individual component `transition: none` overrides are redundant and can be removed from the `.razor.css` files.

- [ ] **Step 1.2: Build to verify no errors**

```bash
dotnet build
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 1.3: Commit**

```bash
git add src/KRAFT.Results.Web/wwwroot/app.css
git commit -m "style(ui): add shared component CSS to app.css"
```

---

## Task 2: Create ErrorMessage component

**Files:**
- Create: `src/KRAFT.Results.Web.Client/Components/ErrorMessage.razor`

- [ ] **Step 2.1: Create the component file**

```razor
@if (OnRetry.HasDelegate)
{
    <div role="alert" class="error-state">
        <p>@Message</p>
        <button class="retry-btn" @onclick="OnRetry">Reyna aftur</button>
    </div>
}
else
{
    <div id="@Id" role="alert" class="error-message">
        <p>@Message</p>
    </div>
}

@code {
    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnRetry { get; set; }

    [Parameter]
    public string? Id { get; set; }
}
```

- [ ] **Step 2.2: Build to verify**

```bash
dotnet build
```

Expected: Build succeeded, 0 error(s).

---

## Task 3: Update non-retry ErrorMessage call sites

Replace the inline error-message markup in pages and forms with `<ErrorMessage>`. Remove the `.error-message` CSS rule and its surrounding media query block from each file's `.razor.css`.

**Files:** AthletesIndex, MeetIndex, TeamsIndex, UserIndex, Login, CreateUserPage, EditUserPage (form-level only), AthleteForm, MeetForm, TeamForm

- [ ] **Step 3.1: AthletesIndex.razor — replace markup**

In `Features/Athletes/AthletesIndex.razor`, replace:
```razor
else if (_errorMessage is not null)
{
    <div role="alert" class="error-message">
        <p>@_errorMessage</p>
    </div>
}
```
With:
```razor
else if (_errorMessage is not null)
{
    <ErrorMessage Message="@_errorMessage" />
}
```

- [ ] **Step 3.2: AthletesIndex.razor — replace empty-state while here**

Replace:
```razor
    @if (_filteredAthletes.Count == 0 && _athletes.Count > 0)
    {
        <div class="empty-state">
            Enginn keppandi fannst.
        </div>
    }
```
With:
```razor
    @if (_filteredAthletes.Count == 0 && _athletes.Count > 0)
    {
        <EmptyState Message="Enginn keppandi fannst." />
    }
```

- [ ] **Step 3.3: AthletesIndex.razor.css — remove duplicate CSS**

Remove these blocks from `Features/Athletes/AthletesIndex.razor.css`:
```css
.empty-state {
    padding: 2rem 1rem;
    text-align: center;
    color: var(--color-text-muted);
    font-size: 0.9375rem;
}
```
```css
.error-message {
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also remove the entire `@media (prefers-reduced-motion)` block for `.btn-action` and `.search-input`:
```css
@media (prefers-reduced-motion: reduce) {
    .btn-action {
        transition: none;
    }

    .search-input {
        transition: none;
    }
}
```
(The global `app.css` media query already handles this.)

- [ ] **Step 3.4: MeetIndex.razor — replace markup**

In `Features/Meets/MeetIndex.razor`, replace:
```razor
@if (_errorMessage is not null)
{
    <div role="alert" class="error-message">
        <p>@_errorMessage</p>
    </div>
}
```
With:
```razor
@if (_errorMessage is not null)
{
    <ErrorMessage Message="@_errorMessage" />
}
```

- [ ] **Step 3.5: MeetIndex.razor.css — remove duplicate CSS**

Remove from `Features/Meets/MeetIndex.razor.css`:
```css
.error-message {
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also remove the `@media (prefers-reduced-motion)` block:
```css
@media (prefers-reduced-motion: reduce) {
    .btn-action {
        transition: none;
    }
}
```

- [ ] **Step 3.6: TeamsIndex.razor — replace markup**

In `Features/Teams/TeamsIndex.razor`, replace:
```razor
@if (_errorMessage is not null)
{
    <div role="alert" class="error-message">
        <p>@_errorMessage</p>
    </div>
}
```
With:
```razor
@if (_errorMessage is not null)
{
    <ErrorMessage Message="@_errorMessage" />
}
```

- [ ] **Step 3.7: TeamsIndex.razor.css — remove duplicate CSS**

Remove from `Features/Teams/TeamsIndex.razor.css`:
```css
.error-message {
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also remove:
```css
@media (prefers-reduced-motion: reduce) {
    .btn-action {
        transition: none;
    }
}
```

- [ ] **Step 3.8: UserIndex.razor — replace markup**

In `Features/Users/UserIndex.razor`, replace:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <div class="error-message" role="alert">@_errorMessage</div>
}
```
With:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <ErrorMessage Message="@_errorMessage" />
}
```

- [ ] **Step 3.9: UserIndex.razor.css — remove duplicate CSS**

Remove from `Features/Users/UserIndex.razor.css`:
```css
.error-message {
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also remove:
```css
@media (prefers-reduced-motion: reduce) {
    .btn-action,
    .btn-edit {
        animation: none;
        transition: none;
    }
}
```
Add back a scoped override for just `.btn-edit` since that class is local to UserIndex:
```css
@media (prefers-reduced-motion: reduce) {
    .btn-edit {
        transition: none;
    }
}
```

- [ ] **Step 3.10: Login.razor — replace markup**

In `Features/Auth/Login.razor`, replace:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <div id="login-error" class="error-message" role="alert">@_errorMessage</div>
}
```
With:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <ErrorMessage Id="login-error" Message="@_errorMessage" />
}
```

- [ ] **Step 3.11: Login.razor.css — remove duplicate CSS**

Remove from `Features/Auth/Login.razor.css`:
```css
.error-message {
    margin-block-start: 1rem;
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also update the `@media (prefers-reduced-motion)` block — remove `.btn-primary` from it (that will be global):
```css
@media (prefers-reduced-motion: reduce) {
    ::deep .form-input {
        transition: none;
    }
}
```
(Previously it was `::deep .form-input, .btn-primary { transition: none; }`)

- [ ] **Step 3.12: CreateUserPage.razor — replace markup**

In `Features/Users/CreateUserPage.razor`, replace:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <div id="create-error" class="error-message" role="alert">@_errorMessage</div>
}
```
With:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <ErrorMessage Id="create-error" Message="@_errorMessage" />
}
```

- [ ] **Step 3.13: CreateUserPage.razor.css — remove duplicate CSS**

Remove from `Features/Users/CreateUserPage.razor.css`:
```css
.error-message {
    margin-block-start: 1rem;
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also update the `@media (prefers-reduced-motion)` block — remove `.btn-primary` from the selector:
```css
@media (prefers-reduced-motion: reduce) {
    ::deep .form-input,
    .btn-cancel {
        transition: none;
    }
}
```
(Previously it also included `.btn-primary`.)

- [ ] **Step 3.14: EditUserPage.razor — replace form-level error markup**

In `Features/Users/EditUserPage.razor`, replace only the form-level error (inside `<EditForm>`):
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <div id="edit-error" class="error-message" role="alert">@_errorMessage</div>
}
```
With:
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <ErrorMessage Id="edit-error" Message="@_errorMessage" />
}
```

**Do NOT change** the page-load error at the bottom of the file (lines 133–138) — it contains a back-link and is kept as custom markup:
```razor
else if (_errorMessage is not null)
{
    <div role="alert" class="error-message">
        <p>@_errorMessage</p>
        <a href="/users">Til baka í notandalista</a>
    </div>
}
```

- [ ] **Step 3.15: EditUserPage.razor.css — remove duplicate CSS**

Remove the `.error-message` block (and any `margin-block-start` wrapper it has) and all `.btn-primary*` blocks from `Features/Users/EditUserPage.razor.css`. The form in EditUserPage now uses `<ErrorMessage>` and `<SubmitButton>` which source their CSS from `app.css`.

- [ ] **Step 3.16: AthleteForm.razor — replace markup**

In `Features/Athletes/AthleteForm.razor`, replace:
```razor
@if (!string.IsNullOrEmpty(ErrorMessage))
{
    <div id="athlete-form-error" class="error-message" role="alert">@ErrorMessage</div>
}
```
With:
```razor
@if (!string.IsNullOrEmpty(ErrorMessage))
{
    <ErrorMessage Id="athlete-form-error" Message="@ErrorMessage" />
}
```

Note: `@ErrorMessage` refers to the component's `ErrorMessage` parameter, not the new `ErrorMessage` component — this is unambiguous in Blazor.

- [ ] **Step 3.17: AthleteForm.razor.css — remove duplicate CSS**

Remove from `Features/Athletes/AthleteForm.razor.css`:
```css
.error-message {
    margin-block-start: 1rem;
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also update the `@media (prefers-reduced-motion)` block — remove `.btn-primary` from the selector list:
```css
@media (prefers-reduced-motion: reduce) {
    ::deep .form-input,
    ::deep .form-select,
    .btn-cancel {
        transition: none;
    }
}
```
(Previously it also included `.btn-primary`.)

- [ ] **Step 3.18: MeetForm.razor — replace markup**

In `Features/Meets/MeetForm.razor`, replace:
```razor
@if (!string.IsNullOrEmpty(ErrorMessage))
{
    <div id="meet-form-error" class="error-message" role="alert">@ErrorMessage</div>
}
```
With:
```razor
@if (!string.IsNullOrEmpty(ErrorMessage))
{
    <ErrorMessage Id="meet-form-error" Message="@ErrorMessage" />
}
```

- [ ] **Step 3.19: MeetForm.razor.css — remove duplicate CSS**

Remove from `Features/Meets/MeetForm.razor.css`:
```css
.error-message {
    margin-block-start: 1rem;
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also update the `@media (prefers-reduced-motion)` block — remove `.btn-primary`:
```css
@media (prefers-reduced-motion: reduce) {
    ::deep .form-input,
    ::deep .form-select,
    .btn-cancel {
        transition: none;
    }
}
```

- [ ] **Step 3.20: TeamForm.razor — replace markup**

Open `Features/Teams/TeamForm.razor`. Find the error message block (same pattern: `!string.IsNullOrEmpty(ErrorMessage)` wrapping a `div id="team-form-error" class="error-message"`). Replace with:
```razor
@if (!string.IsNullOrEmpty(ErrorMessage))
{
    <ErrorMessage Id="team-form-error" Message="@ErrorMessage" />
}
```

- [ ] **Step 3.21: TeamForm.razor.css — remove duplicate CSS**

Remove from `Features/Teams/TeamForm.razor.css`:
```css
.error-message {
    margin-block-start: 1rem;
    padding: 0.75rem;
    background: #fef2f2;
    border: 1px solid #fecaca;
    border-radius: var(--radius-sm);
    color: var(--color-danger);
    font-size: 0.875rem;
    text-align: center;
}
```
Also update the `@media (prefers-reduced-motion)` block — remove `.btn-primary`:
```css
@media (prefers-reduced-motion: reduce) {
    ::deep .form-input,
    ::deep .form-select,
    .btn-cancel {
        transition: none;
    }
}
```

- [ ] **Step 3.22: Build and commit**

```bash
dotnet build
```

Expected: Build succeeded, 0 error(s).

```bash
git add src/KRAFT.Results.Web.Client/Components/ErrorMessage.razor
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthletesIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthletesIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamsIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamsIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Users/UserIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Users/UserIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Auth/Login.razor
git add src/KRAFT.Results.Web.Client/Features/Auth/Login.razor.css
git add src/KRAFT.Results.Web.Client/Features/Users/CreateUserPage.razor
git add src/KRAFT.Results.Web.Client/Features/Users/CreateUserPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Users/EditUserPage.razor
git add src/KRAFT.Results.Web.Client/Features/Users/EditUserPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthleteForm.razor
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthleteForm.razor.css
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetForm.razor
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetForm.razor.css
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamForm.razor
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamForm.razor.css
git commit -m "refactor(ui): introduce ErrorMessage component, replace inline error markup"
```

---

## Task 4: Update retry-variant ErrorMessage call sites

**Files:** RecordsPage, RankingsIndex, RecordHistoryPage, TeamCompetitionIndex

- [ ] **Step 4.1: RecordsPage.razor — replace error-state markup**

In `Features/Records/RecordsPage.razor`, replace:
```razor
else if (_hasError)
{
    <div class="error-state" role="alert">
        <p>Villa kom upp. Reyndu aftur.</p>
        <button class="retry-btn" @onclick="LoadDataAsync">Reyna aftur</button>
    </div>
}
```
With:
```razor
else if (_hasError)
{
    <ErrorMessage Message="Villa kom upp. Reyndu aftur." OnRetry="LoadDataAsync" />
}
```

Also replace the empty-state:
```razor
else
{
    <div class="empty-state">
        Engin met fundust.
    </div>
}
```
With:
```razor
else
{
    <EmptyState Message="Engin met fundust." />
}
```

- [ ] **Step 4.2: RecordsPage.razor.css — remove duplicate CSS**

Remove from `Features/Records/RecordsPage.razor.css`:
```css
.error-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    padding: 2rem 1rem;
    text-align: center;
    color: var(--color-text-muted);
    font-size: 0.9375rem;
}

.retry-btn {
    padding: 0.75rem 1.5rem;
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-body);
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-white);
    background-color: var(--color-primary);
    cursor: pointer;
    transition: background-color var(--transition-fast) ease;
}

.retry-btn:hover {
    background-color: var(--color-primary-dark);
}

.empty-state {
    padding: 2rem 1rem;
    text-align: center;
    color: var(--color-text-muted);
    font-size: 0.9375rem;
}

@media (prefers-reduced-motion: reduce) {
    .retry-btn {
        transition: none;
    }
}
```

- [ ] **Step 4.3: RankingsIndex.razor — replace markup**

In `Features/Rankings/RankingsIndex.razor`, replace:
```razor
else if (_hasError)
{
    <div class="error-state" role="alert">
        <p>Villa kom upp. Reyndu aftur.</p>
        <button class="retry-btn" @onclick="LoadDataAsync">Reyna aftur</button>
    </div>
}
```
With:
```razor
else if (_hasError)
{
    <ErrorMessage Message="Villa kom upp. Reyndu aftur." OnRetry="LoadDataAsync" />
}
```

Also replace:
```razor
else
{
    <div class="empty-state">
        Engar niðurstöður fundust.
    </div>
}
```
With:
```razor
else
{
    <EmptyState Message="Engar niðurstöður fundust." />
}
```

- [ ] **Step 4.4: RankingsIndex.razor.css — remove duplicate CSS**

Remove `.error-state`, `.retry-btn`, `.retry-btn:hover`, `.empty-state`, and the associated `@media (prefers-reduced-motion)` entries from `Features/Rankings/RankingsIndex.razor.css`.

- [ ] **Step 4.5: RecordHistoryPage.razor — replace markup**

In `Features/Records/RecordHistoryPage.razor`, replace:
```razor
else if (_hasError)
{
    <div class="error-state" role="alert">
        <p>Villa kom upp. Reyndu aftur.</p>
        <button class="retry-btn" @onclick="LoadDataAsync">Reyna aftur</button>
    </div>
}
```
With:
```razor
else if (_hasError)
{
    <ErrorMessage Message="Villa kom upp. Reyndu aftur." OnRetry="LoadDataAsync" />
}
```

Also replace:
```razor
else
{
    <div class="empty-state">
        Engar færslur fundust.
    </div>
}
```
With:
```razor
else
{
    <EmptyState Message="Engar færslur fundust." />
}
```

- [ ] **Step 4.6: RecordHistoryPage.razor.css — remove duplicate CSS**

Remove `.error-state`, `.retry-btn`, `.retry-btn:hover`, `.empty-state`, and `@media (prefers-reduced-motion)` entries for these classes from `Features/Records/RecordHistoryPage.razor.css`.

- [ ] **Step 4.7: TeamCompetitionIndex.razor — replace markup**

In `Features/TeamCompetition/TeamCompetitionIndex.razor`, replace:
```razor
else if (_hasError)
{
    <div class="error-state" role="alert">
        <p>Villa kom upp. Reyndu aftur.</p>
        <button class="retry-btn" @onclick="LoadDataAsync">Reyna aftur</button>
    </div>
}
```
With:
```razor
else if (_hasError)
{
    <ErrorMessage Message="Villa kom upp. Reyndu aftur." OnRetry="LoadDataAsync" />
}
```

Also replace:
```razor
        else
        {
            <div class="empty-state">Engin gögn fundust fyrir þetta ar.</div>
        }
```
With:
```razor
        else
        {
            <EmptyState Message="Engin gögn fundust fyrir þetta ar." />
        }
```

- [ ] **Step 4.8: TeamCompetitionIndex.razor.css — remove duplicate CSS**

Remove `.error-state`, `.retry-btn`, `.retry-btn:hover`, `.empty-state`, and `@media (prefers-reduced-motion)` entries for these classes from `Features/TeamCompetition/TeamCompetitionIndex.razor.css`.

- [ ] **Step 4.9: Build and commit**

```bash
dotnet build
```

Expected: Build succeeded, 0 error(s).

```bash
git add src/KRAFT.Results.Web.Client/Components/EmptyState.razor
git add src/KRAFT.Results.Web.Client/Features/Records/RecordsPage.razor
git add src/KRAFT.Results.Web.Client/Features/Records/RecordsPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Rankings/RankingsIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Rankings/RankingsIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Records/RecordHistoryPage.razor
git add src/KRAFT.Results.Web.Client/Features/Records/RecordHistoryPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/TeamCompetition/TeamCompetitionIndex.razor
git add src/KRAFT.Results.Web.Client/Features/TeamCompetition/TeamCompetitionIndex.razor.css
git commit -m "refactor(ui): introduce EmptyState component, replace retry error and empty-state markup"
```

---

## Task 5: Create EmptyState component

**Files:**
- Create: `src/KRAFT.Results.Web.Client/Components/EmptyState.razor`

- [ ] **Step 5.1: Create the component file**

```razor
<div class="empty-state">@Message</div>

@code {
    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;
}
```

Note: This component is already referenced in Task 3 and Task 4 above. Create this file before beginning Task 3 to avoid build errors.

**Important:** Do this step BEFORE Step 3.2 (AthletesIndex EmptyState) and Task 4. Reorder tasks if executing sequentially: create both `ErrorMessage.razor` and `EmptyState.razor` in Task 2, then proceed with call sites.

---

## Task 6: Create PageHeader component and update index page call sites

**Files:**
- Create: `src/KRAFT.Results.Web.Client/Components/PageHeader.razor`
- Modify: AthletesIndex, TeamsIndex, MeetIndex (markup only — CSS is already in app.css from Task 1)
- CSS cleanup: AthletesIndex.razor.css, TeamsIndex.razor.css, MeetIndex.razor.css, UserIndex.razor.css, AthleteDetailsPage.razor.css, MeetDetailsPage.razor.css, TeamDetailsPage.razor.css, EditMeetPage.razor.css

- [ ] **Step 6.1: Create PageHeader.razor**

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
                    <svg class="btn-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
                        <path fill-rule="evenodd" d="M8 2a.5.5 0 0 1 .5.5v5h5a.5.5 0 0 1 0 1h-5v5a.5.5 0 0 1-1 0v-5h-5a.5.5 0 0 1 0-1h5v-5A.5.5 0 0 1 8 2" />
                    </svg>
                    @ActionLabel
                </a>
            </Authorized>
        </AuthorizeView>
    }
</div>

@code {
    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    [Parameter]
    public string? ActionHref { get; set; }

    [Parameter]
    public string? ActionLabel { get; set; }

    [Parameter]
    public string ActionRole { get; set; } = "Admin";
}
```

- [ ] **Step 6.2: AthletesIndex.razor — replace page header**

Replace:
```razor
<div class="page-header">
    <h3>Keppendur</h3>
    <AuthorizeView Roles="Admin">
        <a href="/athletes/create" class="btn-action">
            <svg class="btn-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
                <path fill-rule="evenodd" d="M8 2a.5.5 0 0 1 .5.5v5h5a.5.5 0 0 1 0 1h-5v5a.5.5 0 0 1-1 0v-5h-5a.5.5 0 0 1 0-1h5v-5A.5.5 0 0 1 8 2" />
            </svg>
            Stofna keppanda
        </a>
    </AuthorizeView>
</div>
```
With:
```razor
<PageHeader Title="Keppendur" ActionHref="/athletes/create" ActionLabel="Stofna keppanda" />
```

- [ ] **Step 6.3: AthletesIndex.razor.css — remove duplicate CSS**

Remove from `Features/Athletes/AthletesIndex.razor.css`:
```css
.page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-block-end: 1.25rem;
}

.page-header h3 {
    margin: 0;
}

.btn-action {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    padding: 0.5rem 1rem;
    background: var(--color-primary);
    color: var(--color-white);
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-body);
    font-size: 0.875rem;
    font-weight: 600;
    letter-spacing: 0.02em;
    cursor: pointer;
    text-decoration: none;
    transition: background-color var(--transition-fast) ease;
}

.btn-icon {
    width: 1.125rem;
    height: 1.125rem;
    flex-shrink: 0;
}

.btn-action:hover {
    background: var(--color-primary-dark);
}

.btn-action:focus-visible {
    outline-color: var(--color-text);
}
```

- [ ] **Step 6.4: TeamsIndex.razor — replace page header**

Replace:
```razor
<div class="page-header">
    <h3>Félög</h3>
    <AuthorizeView Roles="Admin">
        <a href="/teams/create" class="btn-action">
            <svg class="btn-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
                <path fill-rule="evenodd" d="M8 2a.5.5 0 0 1 .5.5v5h5a.5.5 0 0 1 0 1h-5v5a.5.5 0 0 1-1 0v-5h-5a.5.5 0 0 1 0-1h5v-5A.5.5 0 0 1 8 2" />
            </svg>
            Stofna félag
        </a>
    </AuthorizeView>
</div>
```
With:
```razor
<PageHeader Title="Félög" ActionHref="/teams/create" ActionLabel="Stofna félag" />
```

- [ ] **Step 6.5: TeamsIndex.razor.css — remove duplicate CSS**

Remove the `.page-header`, `.page-header h3`, `.btn-action`, `.btn-icon`, `.btn-action:hover`, `.btn-action:focus-visible` blocks from `Features/Teams/TeamsIndex.razor.css` (they were already shown in Step 3.7's audit of TeamsIndex.razor.css).

- [ ] **Step 6.6: MeetIndex.razor — replace page header**

Replace:
```razor
<div class="page-header">
    <YearNav Label="Mótaskrá" Year="@Year" BaseUrl="/meets" />
    <AuthorizeView Roles="Admin">
        <a href="/meets/create" class="btn-action">
            <svg class="btn-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
                <path fill-rule="evenodd" d="M8 2a.5.5 0 0 1 .5.5v5h5a.5.5 0 0 1 0 1h-5v5a.5.5 0 0 1-1 0v-5h-5a.5.5 0 0 1 0-1h5v-5A.5.5 0 0 1 8 2" />
            </svg>
            Nýtt mót
        </a>
    </AuthorizeView>
</div>
```
With:
```razor
<PageHeader ActionHref="/meets/create" ActionLabel="Nýtt mót">
    <TitleContent>
        <YearNav Label="Mótaskrá" Year="@Year" BaseUrl="/meets" />
    </TitleContent>
</PageHeader>
```

- [ ] **Step 6.7: MeetIndex.razor.css — remove base CSS, keep local override**

Remove from `Features/Meets/MeetIndex.razor.css` the `.btn-action`, `.btn-icon`, `.btn-action:hover`, `.btn-action:focus-visible` blocks.

**Keep** a local `.page-header` override (MeetIndex uses a bottom border and smaller margin that differs from the global default):
```css
.page-header {
    border-block-end: 1px solid var(--color-border);
    margin-block-end: 1rem;
}
```
(Replace the existing full `.page-header` block with just these two properties.)

- [ ] **Step 6.8: CSS-only cleanup — files that use .btn-action but keep own markup**

These files use `.btn-action` and `.btn-icon` directly in their markup (not through `PageHeader`) — remove the duplicate CSS definitions, they now come from `app.css`.

**UserIndex.razor.css** — remove `.page-header`, `.page-header h3`, `.btn-action`, `.btn-icon`, `.btn-action:hover` blocks. Keep `.btn-edit` and related rules. Keep the scoped `@media (prefers-reduced-motion)` block but only for `.btn-edit`:
```css
@media (prefers-reduced-motion: reduce) {
    .btn-edit {
        transition: none;
    }
}
```

**AthleteDetailsPage.razor.css** — remove these blocks (lines 1–60 of the file):
```css
.page-header { ... }        /* full block */
.btn-action { ... }         /* full block */
.btn-icon { ... }           /* full block */
.btn-action:hover { ... }
.btn-action:focus-visible { ... }
@media (prefers-reduced-motion: reduce) { .btn-action { ... } }
```
Keep `.admin-actions`, `.btn-danger`, `.btn-danger:hover`. Add a local `.page-header` override to preserve the border:
```css
.page-header {
    border-block-end: 1px solid var(--color-border);
    margin-block-end: 1rem;
}
```

**MeetDetailsPage.razor.css** — remove from the top of the file (lines 1–53):
```css
.page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-block-end: 1rem;
    border-block-end: 1px solid var(--color-border);
}

.btn-action { ... }    /* full block, lines 9-25 */
.btn-icon { ... }      /* lines 27-31 */
.btn-action:hover { ... }
.btn-action:focus-visible { ... }
.admin-actions is line 41 — keep this and everything after it.
```
Add a local override:
```css
.page-header {
    border-block-end: 1px solid var(--color-border);
    margin-block-end: 1rem;
}
```

**TeamDetailsPage.razor.css** — remove from the top of the file (lines 1–60):
```css
.page-header { ... }         /* full block, lines 1-7 */
.btn-action { ... }          /* full block, lines 9-25 */
.btn-icon { ... }            /* lines 27-31 */
.btn-action:hover { ... }    /* lines 33-35 */
.btn-action:focus-visible { ... }  /* lines 37-40 */
@media (prefers-reduced-motion: reduce) { .btn-action { ... } }  /* lines 56-60 */
```
Keep `.admin-actions` (line 42), `.btn-danger` (line 48), `.btn-danger:hover` (line 52). Add:
```css
.page-header {
    border-block-end: 1px solid var(--color-border);
    margin-block-end: 1rem;
}
```

**EditMeetPage.razor.css** — this file contains only a context-specific `.error-message` with `max-width: 32rem` and `margin-block-start: 1.5rem` (properties not in `app.css`). Replace the whole file with just the override properties:
```css
.error-message {
    max-width: 32rem;
    margin-block-start: 1.5rem;
}
```
(EditMeetPage uses a back-link in its error div so it keeps its own markup — but the base `.error-message` styling now comes from `app.css`, and this file just adds the page-specific spacing.)

- [ ] **Step 6.9: Build and commit**

```bash
dotnet build
```

Expected: Build succeeded, 0 error(s).

```bash
git add src/KRAFT.Results.Web.Client/Components/PageHeader.razor
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthletesIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthletesIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamsIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamsIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetIndex.razor
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Users/UserIndex.razor.css
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthleteDetailsPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetDetailsPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamDetailsPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Meets/EditMeetPage.razor.css
git commit -m "refactor(ui): introduce PageHeader component, consolidate btn-action CSS"
```

---

## Task 7: Create SubmitButton component and update all form call sites

**Files:**
- Create: `src/KRAFT.Results.Web.Client/Components/SubmitButton.razor`
- Modify: Login, CreateUserPage, EditUserPage, AthleteForm, MeetForm, TeamForm (markup + CSS)

- [ ] **Step 7.1: Create SubmitButton.razor**

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

@code {
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string LoadingLabel { get; set; } = "Augnablik...";
}
```

- [ ] **Step 7.2: Login.razor — replace submit button**

Replace:
```razor
<button type="submit" class="btn-primary" disabled="@_isLoading" aria-busy="@_isLoading">
    @if (_isLoading)
    {
        <span>Augnablik...</span>
    }
    else
    {
        <span>Innskr&#225;</span>
    }
</button>
```
With:
```razor
<SubmitButton Label="Innskrá" IsLoading="@_isLoading" />
```

- [ ] **Step 7.3: Login.razor.css — remove .btn-primary**

Remove from `Features/Auth/Login.razor.css`:
```css
.btn-primary {
    width: 100%;
    padding: 0.875rem 1.5rem;
    background: var(--color-primary);
    color: var(--color-white);
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 600;
    letter-spacing: 0.02em;
    cursor: pointer;
    transition: background-color var(--transition-fast) ease;
}

.btn-primary:hover {
    background: var(--color-primary-dark);
}

.btn-primary:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}
```

- [ ] **Step 7.4: CreateUserPage.razor — replace submit button**

Replace:
```razor
<button type="submit" class="btn-primary" disabled="@_isLoading" aria-busy="@_isLoading">
    @if (_isLoading)
    {
        <span>Augnablik...</span>
    }
    else
    {
        <span>Stofna notanda</span>
    }
</button>
```
With:
```razor
<SubmitButton Label="Stofna notanda" IsLoading="@_isLoading" />
```

- [ ] **Step 7.5: CreateUserPage.razor.css — remove .btn-primary**

Remove from `Features/Users/CreateUserPage.razor.css`:
```css
.btn-primary {
    width: 100%;
    padding: 0.875rem 1.5rem;
    background: var(--color-primary);
    color: var(--color-white);
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 600;
    letter-spacing: 0.02em;
    cursor: pointer;
    transition: background-color var(--transition-fast) ease;
}

.btn-primary:hover {
    background: var(--color-primary-dark);
}

.btn-primary:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}
```

- [ ] **Step 7.6: EditUserPage.razor — replace submit button**

Replace:
```razor
<button type="submit" class="btn-primary" disabled="@_isLoading" aria-busy="@_isLoading">
    @if (_isLoading)
    {
        <span>Augnablik...</span>
    }
    else
    {
        <span>Vista breytingar</span>
    }
</button>
```
With:
```razor
<SubmitButton Label="Vista breytingar" IsLoading="@_isLoading" />
```

- [ ] **Step 7.7: EditUserPage.razor.css — remove .btn-primary**

Remove the `.btn-primary` family from `Features/Users/EditUserPage.razor.css`.

- [ ] **Step 7.8: AthleteForm.razor — replace submit button**

Replace:
```razor
<button type="submit" class="btn-primary" disabled="@IsLoading" aria-busy="@IsLoading">
    @if (IsLoading)
    {
        <span>Augnablik...</span>
    }
    else
    {
        <span>@SubmitLabel</span>
    }
</button>
```
With:
```razor
<SubmitButton Label="@SubmitLabel" IsLoading="@IsLoading" />
```

- [ ] **Step 7.9: AthleteForm.razor.css — remove .btn-primary**

Remove the `.btn-primary`, `.btn-primary:hover`, `.btn-primary:focus-visible`, `.btn-primary:disabled` blocks from `Features/Athletes/AthleteForm.razor.css`.

- [ ] **Step 7.10: MeetForm.razor — replace submit button**

Replace:
```razor
<button type="submit" class="btn-primary" disabled="@IsLoading" aria-busy="@IsLoading">
    @if (IsLoading)
    {
        <span>Augnablik...</span>
    }
    else
    {
        <span>@SubmitLabel</span>
    }
</button>
```
With:
```razor
<SubmitButton Label="@SubmitLabel" IsLoading="@IsLoading" />
```

- [ ] **Step 7.11: MeetForm.razor.css — remove .btn-primary**

Remove from `Features/Meets/MeetForm.razor.css`:
```css
.btn-primary {
    width: 100%;
    padding: 0.875rem 1.5rem;
    background: var(--color-primary);
    color: var(--color-white);
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 600;
    letter-spacing: 0.02em;
    cursor: pointer;
    transition: background-color var(--transition-fast) ease;
}

.btn-primary:hover {
    background: var(--color-primary-dark);
}

.btn-primary:focus-visible {
    outline-color: var(--color-text);
}

.btn-primary:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}
```

- [ ] **Step 7.12: TeamForm.razor — replace submit button**

Open `Features/Teams/TeamForm.razor`. Find the submit button block (same pattern as MeetForm) and replace with:
```razor
<SubmitButton Label="@SubmitLabel" IsLoading="@IsLoading" />
```

- [ ] **Step 7.13: TeamForm.razor.css — remove .btn-primary**

Remove from `Features/Teams/TeamForm.razor.css` (identical content to MeetForm):
```css
.btn-primary {
    width: 100%;
    padding: 0.875rem 1.5rem;
    background: var(--color-primary);
    color: var(--color-white);
    border: none;
    border-radius: var(--radius-sm);
    font-family: var(--font-heading);
    font-size: 1rem;
    font-weight: 600;
    letter-spacing: 0.02em;
    cursor: pointer;
    transition: background-color var(--transition-fast) ease;
}

.btn-primary:hover {
    background: var(--color-primary-dark);
}

.btn-primary:focus-visible {
    outline-color: var(--color-text);
}

.btn-primary:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}
```

- [ ] **Step 7.14: Build and commit**

```bash
dotnet build
```

Expected: Build succeeded, 0 error(s).

```bash
git add src/KRAFT.Results.Web.Client/Components/SubmitButton.razor
git add src/KRAFT.Results.Web.Client/Features/Auth/Login.razor
git add src/KRAFT.Results.Web.Client/Features/Auth/Login.razor.css
git add src/KRAFT.Results.Web.Client/Features/Users/CreateUserPage.razor
git add src/KRAFT.Results.Web.Client/Features/Users/CreateUserPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Users/EditUserPage.razor
git add src/KRAFT.Results.Web.Client/Features/Users/EditUserPage.razor.css
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthleteForm.razor
git add src/KRAFT.Results.Web.Client/Features/Athletes/AthleteForm.razor.css
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetForm.razor
git add src/KRAFT.Results.Web.Client/Features/Meets/MeetForm.razor.css
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamForm.razor
git add src/KRAFT.Results.Web.Client/Features/Teams/TeamForm.razor.css
git commit -m "refactor(ui): introduce SubmitButton component, consolidate btn-primary CSS"
```

---

## Task 8: Final verification

- [ ] **Step 8.1: Run a full build**

```bash
dotnet build
```

Expected: Build succeeded, 0 error(s), 0 warning(s) that weren't present before.

- [ ] **Step 8.2: Grep for leftover duplicate class definitions**

Run these to confirm no stale definitions remain in component CSS files:

```bash
grep -r "\.error-message" src/KRAFT.Results.Web.Client --include="*.razor.css"
grep -r "\.error-state" src/KRAFT.Results.Web.Client --include="*.razor.css"
grep -r "\.empty-state" src/KRAFT.Results.Web.Client --include="*.razor.css"
grep -r "\.btn-primary" src/KRAFT.Results.Web.Client --include="*.razor.css"
```

Expected: No output (all these classes now live only in `app.css`).

```bash
grep -r "\.btn-action" src/KRAFT.Results.Web.Client --include="*.razor.css"
grep -r "\.page-header" src/KRAFT.Results.Web.Client --include="*.razor.css"
```

Expected: Only the local override instances in `MeetIndex.razor.css`, `AthleteDetailsPage.razor.css`, `MeetDetailsPage.razor.css`, `TeamDetailsPage.razor.css` (just `border-block-end` / `margin-block-end` overrides, not full definitions).

- [ ] **Step 8.3: Final commit if any cleanup was done**

If the grep in 8.2 revealed any remaining duplicates, remove them and commit:

```bash
git add -u
git commit -m "refactor(ui): remove remaining duplicate CSS class definitions"
```

---

## Ordering note

Tasks 3 and 5 both reference `EmptyState`. Create the `EmptyState.razor` file at the start of Task 3 (not Task 5 as sequenced above) to avoid build errors during Task 3. Similarly, create `ErrorMessage.razor` before Step 3.1.

Revised creation order:
1. Task 1 — add CSS to `app.css`
2. Create `ErrorMessage.razor` and `EmptyState.razor` (Tasks 2 and 5)
3. Task 3 — update non-retry ErrorMessage + EmptyState in AthletesIndex
4. Task 4 — update retry ErrorMessage + EmptyState in remaining pages
5. Task 6 — PageHeader
6. Task 7 — SubmitButton
7. Task 8 — verification
