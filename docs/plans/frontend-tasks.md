# Frontend implementation tasks — Pull Up Angular

Vertically sliced tasks for FI1. Each task ships an end-to-end UI slice — route → component(s) in the right library → service in `api` via its `*.service.contract.ts` → live integration → Playwright POM acceptance test. Sizing target: 1–3 loop iterations per task. Tasks reference `./docs/plans/frontend.md` sections and `./docs/specs/L2.md` IDs.

## Conventions for every task in this list

Unless a task explicitly says otherwise, **every task in this list**:

- Conforms to Implementation Guidance sections: Frontend (Angular), Library Structure, Authentication (frontend side), Testing, and General. The MF1-given conventions (BEM, one-type-per-file with separate `.html`/`.scss`/`.ts`, Material 3 components only, design tokens via `--mat-sys-*` + `--pu-space-*`, interface-driven services via `*.service.contract.ts`) are baseline — no per-task re-litigation.
- Places artifacts in the correct library (FP1 §1 dependency direction):
  - **`components`** — reusable presentation components, no api dependency.
  - **`api`** — backend-facing services + DTOs + contracts.
  - **`domain`** — feature pages that consume api services through their tokens.
  - **`pull-up` app** — routes, providers, guards, interceptors.
- Adds the Playwright POM under `frontend/e2e/pages/` and the spec under `frontend/e2e/`, with a header comment naming the L2 IDs it covers.
- ATDD: the failing Playwright spec is committed first, then implementation, then green.
- Mocks backend HTTP via `page.route('**/api/...', ...)` so e2e runs without a live .NET API.
- Zero stubs: no `TODO`, no `console.log` debug, no `bypassSecurityTrust*` on user content.
- Build clean (`ng build pull-up` reports 0 warnings, 0 errors) and lint clean.

## A note on task shape — "vertical slice" vs "supporting infrastructure"

Same pattern as `backend-tasks.md`. Bands A and B are **supporting infrastructure** — they ship api services and components used by downstream pages, not their own routable surface. Each one advances real L2 requirements (the L2s it enables for downstream slices) and has its own integration verification (component unit tests via Angular's built-in `TestBed` where useful + downstream Playwright runs as smoke). Folding band-A or band-B work into the first consuming page would push that page well past the sizing rule.

Bands C–F are true vertical slices: each ships a route + UI + at least one new Playwright POM spec.

---

## A. api library — backend-facing services (supporting infrastructure)

### FT-001: AuthService completion + AuthStorage refactor — *enables* L2-004, L2-005, L2-006, L2-007, L2-008, L2-009 — **done**
- **Slice contents:** in `projects/api/src/lib/auth/`: add `signIn`, `requestPasswordReset`, `completePasswordReset`, `refresh`, `signOut` to `AuthService` + `IAuthService`; add DTOs `SignInRequest/Response`, `RequestPasswordResetRequest`, `CompletePasswordResetRequest`, `RefreshAccessTokenRequest/Response`, `SignOutRequest`. Refactor token persistence out of `AuthService` into `AuthStorage` (single-responsibility wrapper around `localStorage`).
- **Acceptance test:** unit test in `frontend/projects/api/src/lib/auth/auth.service.spec.ts` (TestBed + `HttpTestingController`): each new method posts to the expected path with the expected body and parses the response shape.
- **Guidance:** Frontend, Library Structure (api).

### FT-002: ProfileService — *enables* L2-012, L2-013, L2-014, L2-015, L2-016, L2-017 — **done**
- **Slice contents:** new `projects/api/src/lib/profile/` folder with `profile.service.{ts,contract.ts}`; DTOs `UpdateProfileRequest`, `RequestEmailChangeRequest`, `ConfirmEmailChangeRequest`, `DeleteAccountRequest`, `NotificationPreferences`, `UpdateNotificationPreferencesRequest`; `IProfileService` exposes `updateProfile`, `requestEmailChange`, `confirmEmailChange`, `deleteAccount`, `getNotificationPreferences`, `updateNotificationPreferences`.
- **Acceptance test:** `profile.service.spec.ts` — one TestBed case per method asserting URL + verb + body shape.
- **Guidance:** Frontend, Library Structure (api).

### FT-003: EventsService — *enables* L2-018..L2-036, L2-039 — **done**
- **Slice contents:** new `projects/api/src/lib/events/` folder with `events.service.{ts,contract.ts}`; DTOs `EventSummary`, `ListMyEventsResponse`, `EventDetail`, `HostSummary`, `GuestSummary`, `CreateEventRequest/Response`, `UpdateEventRequest`, `AddInviteeRequest`, `SetRsvpRequest`, `RsvpStatus` (string union); `IEventsService` exposes `list`, `get`, `create`, `update`, `cancel`, `addInvitee`, `removeInvitee`, `setRsvp`.
- **Acceptance test:** `events.service.spec.ts` — one TestBed case per method.
- **Guidance:** Frontend, Library Structure (api).

### FT-004: Interceptor 401-refresh + error interceptor + app providers — *enables* L2-006 — **done**
- **Slice contents:** extend `auth-jwt.interceptor.ts` so a 401 response triggers exactly one `AuthService.refresh(refreshToken)` retry; on refresh failure clear tokens + redirect to `/sign-in`. New `error.interceptor.ts` surfaces non-form errors via `MatSnackBar`. Update `app.config.ts` to provide `PROFILE_SERVICE` + `EVENTS_SERVICE` tokens and register both interceptors.
- **Acceptance test:** `auth-jwt-interceptor.spec.ts` (TestBed + `HttpTestingController`): a 401 on a protected endpoint triggers exactly one refresh attempt; on refresh success the original request is retried with the new bearer; on refresh failure tokens are cleared and `/sign-in` is navigated to.
- **Guidance:** Frontend, Authentication.

---

## B. components library buildout (supporting infrastructure)

### FT-005: App-shell components — *enables* L2-052 — **done**
- **Slice contents:** in `projects/components/src/lib/`: `app-bar/app-bar.component.{ts,html,scss}` (leading icon slot, title input, trailing avatar slot), `bottom-nav-bar/bottom-nav-bar.component.{ts,html,scss}` (hidden ≥768 via CSS), `nav-rail/nav-rail.component.{ts,html,scss}` (shown ≥768 via CSS).
- **Acceptance test:** Playwright responsive smoke (lands later in FT-020) verifies the swap at 768px; component-level inspection by composition in band D / E / F pages.
- **Guidance:** Frontend, Library Structure (components imports nothing from api / domain).

### FT-006: Event card + RSVP avatar stack — *enables* L2-022, L2-023 — **done**
- **Slice contents:** `event-card/event-card.component.{ts,html,scss}` takes **primitive inputs only** (`title: string`, `startsAtUtc: Date`, `location: string`, `isHost: boolean`, `myRsvpStatus: 'Going' | 'Maybe' | 'CantGo' | null`) so the `components` library imports nothing from `api`. The consuming page (FT-015 home, FT-017 detail) maps `EventSummary` → these primitives. `rsvp-avatar-stack/rsvp-avatar-stack.component.{ts,html,scss}` takes an array of `{ initials: string; tone: 'primary' | 'secondary' | 'tertiary' }` — also primitives.
- **Acceptance test:** indirectly verified by FT-015 (home flow uses event cards) and FT-017 (event detail uses avatar stack).
- **Guidance:** Frontend, Library Structure (components imports nothing from api/domain — verified by import grep in FT2 + FI1 evals). The "primitive inputs only" rule mirrors how `EventFormComponent` is placed in `domain` because **it** binds to api DTOs — same logic in reverse here keeps `components` clean.

### FT-007: State components — *enables* L2-062, L2-063 — **done**
- **Slice contents:** `empty-state/empty-state.component.{ts,html,scss}` (icon + title + supporting text + CTA slot), `error-state/error-state.component.{ts,html,scss}` (same shape, error theme + retry CTA), `loading-skeleton/loading-skeleton.component.{ts,html,scss}` (Material 3 skeleton).
- **Acceptance test:** indirectly verified by FT-015 (home empty + error + loading paths).
- **Guidance:** Frontend, Library Structure.

### FT-008: Interactive components — *enables* L2-025, L2-034
- **Slice contents:** `filter-strip/filter-strip.component.{ts,html,scss}` (horizontal scroll chips, inputs `chips` + `selectedKey`, output `chipChange`), `segmented-button/segmented-button.component.{ts,html,scss}` (M3 segmented control for RSVP picker).
- **Acceptance test:** indirectly verified by FT-015 + FT-017.
- **Guidance:** Frontend, Library Structure.

---

## C. Auth feature pages (vertical slices)

### FT-009: Sign-in page + flow — **L2-004, L2-005**
- **Slice contents:** new `projects/domain/src/lib/sign-in-page/sign-in-page.component.{ts,html,scss}`. Reactive form with email + password. Wires `@Inject(AUTH_SERVICE) auth: IAuthService` and calls `auth.signIn(...)`. On 200 stores tokens and routes to `/home`. On 401 surfaces generic "Invalid email or password." Routes registered in `app.routes.ts` at `/sign-in`. Composes `BrandLogoComponent`.
- **Acceptance test:** `frontend/e2e/sign-in-flow.spec.ts` + `pages/sign-in-page.ts` — happy path stores tokens and navigates to home; wrong creds shows the generic 401 message and stays on /sign-in.
- **Guidance:** Frontend, Library Structure (domain), Authentication, Testing.

### FT-010: Password-reset flow — **L2-008, L2-009, L2-003**
- **Slice contents:** new `request-password-reset-page` + `complete-password-reset-page` domain components. `RequestPasswordResetPage` always shows the same "If an account exists…" success state after submit. `CompletePasswordResetPage` reads `?token=` from the URL, requires the new password + complexity rules. Routes `/password-reset` and `/password-reset/confirm`.
- **Acceptance test:** `password-reset-flow.spec.ts` + two POMs — request submission shows the universal success state; confirm with a token transitions to `/sign-in` with a snackbar; confirm with a stale token surfaces the 400 error.
- **Guidance:** Frontend, Authentication.

---

## D. Account pages (vertical slices)

### FT-011: Profile page (view + edit name) — **L2-011, L2-012**
- **Slice contents:** new `profile-page` domain component composing `AppBarComponent`, the M3 `mat-form-field` triple (full name / display name / email read-only), an "Edit" button that opens a `MatDialog` with the reactive form, an "Edit profile" button that posts to `PROFILE_SERVICE.updateProfile`. Route `/profile`. Includes the `BottomNavBarComponent` / `NavRailComponent` responsive nav.
- **Acceptance test:** `profile-edit-flow.spec.ts` + `ProfilePagePom` — load /profile, click edit, change full name, save, reload — new name shown on the page.
- **Guidance:** Frontend, Authentication (must be authed to view), Library Structure.

### FT-012: Notification preferences toggles on Profile — **L2-016, L2-017**
- **Slice contents:** add a "Notifications" section to `ProfilePage` showing three M3 `mat-slide-toggle`s (New invitations / Event reminders / RSVP changes). Calls `PROFILE_SERVICE.getNotificationPreferences()` on mount; `updateNotificationPreferences` on toggle change (debounced ≤ 500 ms). Defaults all-on for new users.
- **Acceptance test:** extend `profile-edit-flow.spec.ts` (or split into a `notification-preferences.spec.ts`) — new user lands with all on; toggling persists across reload.
- **Guidance:** Frontend, Library Structure.

### FT-013: Email change request + confirm — **L2-013**
- **Slice contents:** add a "Change email" affordance to `ProfilePage` that opens a `MatDialog` for new email + current password and calls `PROFILE_SERVICE.requestEmailChange(...)`. New `confirm-email-change-page` domain component reads `?token=` and calls `PROFILE_SERVICE.confirmEmailChange(...)`. Route `/email-change/confirm`.
- **Acceptance test:** `email-change-flow.spec.ts` + two POMs (`ProfilePagePom` extension + `ConfirmEmailChangePagePom`) — request flow shows verification success snackbar; confirm page reads the token from the URL and on success the profile shows the new email.
- **Guidance:** Frontend, Authentication.

### FT-014: Delete account page — **L2-014, L2-015**
- **Slice contents:** new `delete-account-page` domain component reached from a destructive link on `ProfilePage`. Reactive form with current password + an "I understand" checkbox. Calls `PROFILE_SERVICE.deleteAccount(...)`. On 204 clears tokens (via `AuthService.signOut` semantics) and routes to `/sign-up` with a "Account deleted" snackbar. Route `/profile/delete`.
- **Acceptance test:** `delete-account-flow.spec.ts` + `DeleteAccountPagePom` — wrong password keeps user on the page with an error; correct password navigates to /sign-up; subsequent attempt to call /me with the cleared bearer fails (via captured logger).
- **Guidance:** Frontend, Authentication.

---

## E. Events pages (vertical slices)

### FT-015: Home page full event list — **L2-022, L2-024, L2-025, L2-062, L2-063**
- **Slice contents:** replace MF1's welcome card with a full list view. Compose `AppBarComponent`, `BottomNavBarComponent`, `NavRailComponent`, `FilterStripComponent` (All / Hosting / Invited / Past), `EventCardComponent` per row, `LoadingSkeletonComponent` on load, `EmptyStateComponent` on zero results, `ErrorStateComponent` on fetch error. Calls `EVENTS_SERVICE.list(scope)`.
- **Acceptance test:** extend `sign-up-flow.spec.ts` / new `home-list-flow.spec.ts` — after sign-up, mocked /api/events returns a populated grouped list; cards render in the right buckets; switching filter chips re-fetches with the new scope.
- **Guidance:** Frontend, Library Structure, Testing.

### FT-016: Event create page — **L2-018..L2-021, L2-031, L2-032**
- **Slice contents:** new `event-create-page` domain component. Reactive form: title, date (M3 `mat-datepicker`), time, location, description, options toggles (allow +1, show guest list), invitees chip-input. Calls `EVENTS_SERVICE.create(...)`. On 201 routes to `/events/:id` of the new event. Route `/events/new`.
- **Acceptance test:** `event-create-flow.spec.ts` + `EventCreatePagePom` — fill form, submit, mocked create returns 201 with id, page navigates to `/events/{id}` and detail fetches.
- **Guidance:** Frontend, Library Structure.

### FT-017: Event detail page (read + RSVP) — **L2-023, L2-034, L2-035, L2-036**
- **Slice contents:** new `event-detail-page` domain component composing `AppBarComponent` (back arrow + share/more icons), the hero card with title + date + time + location, the M3 `SegmentedButtonComponent` for "Your RSVP", description card, host card with name + initials, guest list driven by `GuestSummary[]` from `EventsService.get`. RSVP picker calls `EVENTS_SERVICE.setRsvp(...)` and refreshes counts. Route `/events/:id`.
- **Acceptance test:** `event-rsvp-flow.spec.ts` + `EventDetailPagePom` — open detail, click Maybe, mocked PUT returns 204, count chips re-render; for a past event the RSVP segmented control is hidden.
- **Guidance:** Frontend, Library Structure.

### FT-018: Event cancel action — **L2-029, L2-030**
- **Slice contents:** on `EventDetailPage`, host-only "Cancel event" `mat-stroked-button color="warn"` with a confirm `MatDialog`. Calls `EVENTS_SERVICE.cancel(id)`. On 204 reloads the event so the page reflects `Status=Cancelled` (banner / disabled RSVP).
- **Acceptance test:** `event-cancel-flow.spec.ts` extension — host sees the Cancel button, clicks, confirms; the page renders the cancelled-status banner; non-host view does not show the button.
- **Guidance:** Frontend, Library Structure.

### FT-019: Event edit + invitee management — **L2-026, L2-027, L2-028, L2-031, L2-032, L2-033**
- **Slice contents:** new `event-edit-page` domain component reusing a presentational `event-form-component` (kept in `domain` because it binds to api DTOs by type) for the title/date/time/location/description/options form. Plus an invitee section with chip-input for adding (calls `EVENTS_SERVICE.addInvitee(...)`) and an `(x)` per chip for removing (calls `EVENTS_SERVICE.removeInvitee(...)`). Route `/events/:id/edit`. Host-only — the page reads `IsHost` from the detail fetch on mount and otherwise routes back to detail.
- **Acceptance test:** `event-update-flow.spec.ts` + `EventEditPagePom` — host edits date, saves, redirects to detail, new date renders; host adds an invitee, chip appears; host removes invitee, chip disappears.
- **Guidance:** Frontend, Library Structure, Authentication (host-only client-side gate + server-side 403).

---

## F. Cross-cutting test coverage

### FT-020: Responsive viewport spec — **L2-051..L2-054**
- **Slice contents:** extend the existing 360-mobile assertion into a dedicated `responsive.spec.ts` that walks `/sign-in`, `/home`, `/events/{id}`, `/profile` at 360 / 768 / 1440 widths and asserts (a) the bottom nav-bar is visible <768 + hidden ≥768, (b) the nav-rail is visible ≥768 + hidden <768, (c) `document.documentElement.scrollWidth === clientWidth` (no horizontal overflow), (d) primary action button is in the viewport without scroll on each width.
- **Acceptance test:** the spec itself (self-verifying).
- **Guidance:** Frontend, Testing.

---

## G. Sequencing and dependency graph

```
A. FT-001 (AuthService extensions) ──┐
   FT-002 (ProfileService)           ├── prerequisites for C, D, E pages
   FT-003 (EventsService)            │
   FT-004 (Interceptor + providers) ─┘

B. FT-005 (app-shell components)  ───┐
   FT-006 (event card + avatar)      ├── prerequisites for E pages + responsive spec
   FT-007 (state components)         │
   FT-008 (interactive components)  ─┘

C. FT-009 (Sign-in)  ── needs FT-001
   FT-010 (Password reset)  ── needs FT-001

D. FT-011 (Profile view+edit) ── needs FT-002 + FT-005
   FT-012 (Notification prefs) ── needs FT-002, FT-011
   FT-013 (Email change)       ── needs FT-002, FT-011
   FT-014 (Delete account)     ── needs FT-002

E. FT-015 (Home list)        ── needs FT-003, FT-005, FT-006, FT-007, FT-008
   FT-016 (Event create)     ── needs FT-003, FT-005
   FT-017 (Event detail+RSVP)── needs FT-003, FT-005, FT-008
   FT-018 (Event cancel)     ── needs FT-017
   FT-019 (Event edit)       ── needs FT-016, FT-017

F. FT-020 (Responsive spec)  ── needs FT-015 + FT-017 + FT-011 (so all pages exist)
```

Recommended order for FI1: A → B → C → D → E → F, top-to-bottom inside each band. Cross-band dependencies are the only hard constraints.

## H. Sizing

20 tasks. Each one fits in 1–3 loop iterations:

- **A.** ~3-4 files per service + spec ≈ one iteration each (FT-001 is the largest because it extends an existing service across 5 methods; could split if FI1 finds it tight).
- **B.** Each presentation-component task is 2–3 components × 3 files (`.ts`/`.html`/`.scss`) ≈ one iteration.
- **C–E.** Each page-slice task is 1 component (3 files) + route registration + service consumption + 1 POM + 1 spec ≈ one iteration.
- **F.** A single spec ≈ one iteration.

Total target: ~25–30 loop iterations to complete FI1. The four supporting-infrastructure tasks at the top (FT-001..FT-004) plus four component tasks (FT-005..FT-008) front-load the work; the page slices then move briskly.

No task introduces a state-management library, a presenter pattern, a feature NgModule, or a `bypassSecurityTrust*` call. Each task is radically simple within its own boundary.
