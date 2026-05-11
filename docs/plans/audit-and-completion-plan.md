# Pull Up — Implementation Audit & Completion Plan

Audit date: 2026-05-11
Sources audited: `docs/specs/L1.md`, `docs/specs/L2.md`, `docs/mocks/`,
`docs/plans/backend-tasks.md`, `docs/plans/frontend-tasks.md`,
`docs/evaluations/`, `backend/`, `frontend/`.

---

## 1. Audit summary

### 1.1 Backend (`docs/plans/backend-tasks.md`) — **complete**

All 23 vertically-sliced backend tasks (BT-001..BT-023) plus the two MB1
prerequisite slices (BT-024 RegisterUser, BT-025 GetCurrentUser) are
marked **DONE** in the plan and have a matching evaluation file under
`docs/evaluations/BI1-BT-001.md` … `BI1-BT-023.md`. Code in
`backend/src/PullUp.Application/Features/` and integration tests under
`backend/tests/PullUp.Api.IntegrationTests/` confirm presence of:

- Cross-cutting: `AuthorizationBehavior`, `AuditingBehavior`,
  request-body redaction, HSTS + `/health/live` + `/health/ready`.
- Auth feature: `sign-in`, refresh-token store + `Pbkdf2TokenHasher`,
  `SignInRateLimiter`, `refresh`, `sign-out`, `password-reset`
  request + confirm.
- Users feature: profile edit, email-change request + confirm,
  notification-preferences PUT, account deletion.
- Events feature: entity + create + list + detail + cancel + edit +
  invitee add/remove + RSVP PUT + 24-hour reminder hosted service +
  invitation-notification dispatch via `INotificationSender`.

**No outstanding backend work** for FI1/BI1 scope. Any new backend slice
would be a follow-on iteration outside the BI1 plan.

### 1.2 Frontend (`docs/plans/frontend-tasks.md`) — **partial**

| Band | Task | Status | Verification |
| --- | --- | --- | --- |
| A | FT-001 AuthService + AuthStorage | done | `BI1-FT-001` eval, `auth.service.ts` |
| A | FT-002 ProfileService | done | `BI1-FT-002` eval |
| A | FT-003 EventsService | done | `BI1-FT-003` eval |
| A | FT-004 401-refresh + error interceptors + providers | done | `auth-jwt.interceptor.ts`, `error.interceptor.ts` |
| B | FT-005 App-shell components (app-bar, bottom-nav, nav-rail) | done | `projects/components/src/lib/{app-bar,bottom-nav-bar,nav-rail}` |
| B | FT-006 Event card + RSVP avatar stack | done | `event-card`, `rsvp-avatar-stack` |
| B | FT-007 Empty / Error / Loading-skeleton | done | `empty-state`, `error-state`, `loading-skeleton` |
| B | FT-008 Filter strip + Segmented button | done | `filter-strip`, `segmented-button` |
| C | FT-009 Sign-in page | done | `sign-in-page`, `sign-in-flow.spec.ts` |
| C | FT-010 Password-reset flow | done | `request-password-reset-page`, `complete-password-reset-page`, `password-reset-flow.spec.ts` |
| D | FT-011 Profile view + edit | done | `profile-page`, `profile-edit-flow.spec.ts` |
| D | FT-012 Notification preferences toggles | done | `notification-preferences.spec.ts` |
| D | FT-013 Email-change request + confirm | done | `confirm-email-change-page`, `email-change-flow.spec.ts` |
| **D** | **FT-014 Delete-account page** | **NOT DONE** | no `delete-account-page` directory; not in `app.routes.ts`; no eval file |
| **E** | **FT-015 Home full event list** | **NOT DONE** | `HomePageComponent` is the MF1 welcome card calling `auth.loadCurrentUser()`, not `EventsService.list(...)` |
| **E** | **FT-016 Event create page** | **NOT DONE** | no `event-create-page`; route `/events/new` missing |
| **E** | **FT-017 Event detail + RSVP** | **NOT DONE** | no `event-detail-page`; route `/events/:id` missing |
| **E** | **FT-018 Event cancel action** | **NOT DONE** | depends on FT-017 |
| **E** | **FT-019 Event edit + invitee mgmt** | **NOT DONE** | no `event-edit-page`; route `/events/:id/edit` missing |
| **F** | **FT-020 Responsive viewport spec** | **NOT DONE** | no `responsive.spec.ts` in `frontend/e2e/` |

Cross-check against mocks (`docs/mocks/`):

- `home.html` (with filter chips + event cards + FAB) — **not realized** in the app; current `/home` shows a welcome card.
- `event-detail.html` — **not realized**; no detail route.
- `event-create.html` — **not realized**; no create/edit route.
- `empty-state.html` / `error-state.html` — components exist but the
  consuming home flow that surfaces them is FT-015 and is not built.
- `profile.html` — realized via `profile-page` (FT-011/012/013); the
  destructive "Delete account" link from the mock points at the missing
  FT-014 page.
- `sign-in.html`, `sign-up.html`, `password-reset.html` — realized.

### 1.3 L2 requirement coverage gap (driven by missing FT)

The following L2 IDs from `docs/specs/L2.md` are **served by the backend
but not yet by the frontend** because the consuming Angular slice is
unbuilt:

- L2-014, L2-015 — account deletion UI flow (FT-014).
- L2-018..L2-021 — event creation form (FT-016).
- L2-022, L2-024, L2-025, L2-062, L2-063 — home list, filter chips,
  empty/error/loading on the home (FT-015).
- L2-023, L2-034, L2-035, L2-036 — event detail and RSVP picker
  (FT-017).
- L2-029, L2-030 — host cancel action (FT-018).
- L2-026..L2-028, L2-031..L2-033 — host edit + invitee add/remove
  (FT-019).
- L2-051..L2-054 — responsive spec coverage for the new pages (FT-020).

No L1 requirement is fully un-served once these seven tasks land; the
backend already implements every corresponding `IAppDbContext` /
endpoint surface.

### 1.4 Items explicitly **not** in scope of this completion plan

- New backend slices, additional L1/L2 items, or any change to the
  `docs/specs/` source of truth.
- Replacing the mocks under `docs/mocks/` — they are reference-only.
- Refactors to already-passing FT-001..FT-013 work unless touched
  incidentally by a remaining FT slice.

---

## 2. Completion plan

Order follows the dependency graph in
`docs/plans/frontend-tasks.md` §G. All conventions in
`docs/plans/frontend-tasks.md` §"Conventions for every task in this
list" still apply (BEM, separate `.html`/`.scss`/`.ts`, Material 3 only,
DI via `*.service.contract.ts` tokens, Playwright POMs, ATDD: failing
spec first, mock backend with `page.route('**/api/...')`, zero stubs,
clean build + lint).

### Task FC-1 (= FT-014) — Delete-account page
- **Implements:** L2-014, L2-015.
- **Files to add:**
  - `frontend/projects/domain/src/lib/delete-account-page/delete-account-page.component.{ts,html,scss}`
  - export from `projects/domain/src/public-api.ts`
  - route `/profile/delete` in `projects/pull-up/src/app/app.routes.ts` behind `authGuard`
  - destructive link on `ProfilePageComponent` template ("Delete account") that routes to `/profile/delete`
  - `frontend/e2e/pages/delete-account-page.ts` POM
  - `frontend/e2e/delete-account-flow.spec.ts`
- **Behaviour:** reactive form (current password + "I understand" checkbox required); on success calls `PROFILE_SERVICE.deleteAccount(...)`, clears tokens via `AuthStorage`, navigates to `/sign-up` with snackbar "Account deleted." On 401 surface inline error.
- **Acceptance test (ATDD):** wrong password → stays on page with error; correct password → navigated to `/sign-up`; subsequent `/api/users/me` request fails (interceptor cleared bearer).
- **Eval doc:** `docs/evaluations/FI1-FT-014.md`.

### Task FC-2 (= FT-015) — Home page full event list
- **Implements:** L2-022, L2-024, L2-025, L2-062, L2-063.
- **Replaces** the current welcome-card body of `HomePageComponent`.
- **Composes:** `AppBarComponent`, `BottomNavBarComponent`, `NavRailComponent`, `FilterStripComponent` (chips: All / Hosting / Invited / Past), `EventCardComponent`, `LoadingSkeletonComponent`, `EmptyStateComponent`, `ErrorStateComponent`. FAB → `/events/new`.
- **Wires** `@Inject(EVENTS_SERVICE) events: IEventsService`; calls `events.list(scope)` on init and on chip change. Renders the response groups (`thisWeek`, `laterThisMonth`, `nextMonth`, `past`) as separate sections.
- **Acceptance test:** new `frontend/e2e/home-list-flow.spec.ts` + `HomePagePom` extension. Mocks `/api/events?scope=...` for empty / populated / error variants; asserts grouped buckets, filter re-fetch, FAB navigates to `/events/new`.
- **Eval doc:** `docs/evaluations/FI1-FT-015.md`.

### Task FC-3 (= FT-016) — Event create page
- **Implements:** L2-018..L2-021, L2-031, L2-032.
- **Files:** `event-create-page` domain component (3 files), export, route `/events/new` behind `authGuard`, `EventCreatePagePom`, `event-create-flow.spec.ts`.
- **Form:** title, date (`mat-datepicker`), time (`mat-form-field` with `type="time"` input or M3 timepicker if available), location, description, allow-+1 toggle, show-guest-list toggle, invitee chip-input (emails). FluentValidation-equivalent client rules to surface server 400 cleanly.
- **Behaviour:** on submit calls `EVENTS_SERVICE.create(...)`; on 201 navigates to `/events/{id}`. On 400 renders field errors from the `ValidationProblemDetails` payload.
- **Acceptance test:** mocked `POST /api/events` returns 201 with `id`; assert URL changes to `/events/{id}` and detail call fires (mocked).
- **Eval doc:** `docs/evaluations/FI1-FT-016.md`.

### Task FC-4 (= FT-017) — Event detail page + RSVP
- **Implements:** L2-023, L2-034, L2-035, L2-036.
- **Files:** `event-detail-page` domain component, route `/events/:id` behind `authGuard`, `EventDetailPagePom`, `event-rsvp-flow.spec.ts`.
- **Composes:** `AppBarComponent` (back arrow + share/more icons), hero card (title/date/time/location), `SegmentedButtonComponent` for RSVP picker (Going / Maybe / Can't go), description card, host card, guest list (`RsvpAvatarStackComponent`).
- **Behaviour:** `EVENTS_SERVICE.get(id)` on init; `EVENTS_SERVICE.setRsvp(...)` on selection change → re-fetch detail to refresh aggregate counts. Past events hide the RSVP control.
- **Acceptance test:** open detail, click "Maybe", mocked `PUT /api/events/{id}/rsvp` returns 204, assert count chips re-render. Past event variant hides the picker.
- **Eval doc:** `docs/evaluations/FI1-FT-017.md`.

### Task FC-5 (= FT-018) — Event cancel action
- **Implements:** L2-029, L2-030.
- **Depends on:** FC-4.
- **Files:** extend `EventDetailPageComponent` with a host-only "Cancel event" `mat-stroked-button color="warn"`; confirm `MatDialog`; calls `EVENTS_SERVICE.cancel(id)`; on 204 re-fetch and render cancelled banner + disable RSVP. Extend `EventDetailPagePom`. Add `event-cancel-flow.spec.ts` (or extend the FC-4 spec).
- **Acceptance test:** host variant sees button → confirms → cancelled banner. Non-host variant does not see the button.
- **Eval doc:** `docs/evaluations/FI1-FT-018.md`.

### Task FC-6 (= FT-019) — Event edit + invitee management
- **Implements:** L2-026, L2-027, L2-028, L2-031, L2-032, L2-033.
- **Depends on:** FC-3, FC-4.
- **Files:**
  - `event-edit-page` domain component, route `/events/:id/edit` behind `authGuard`.
  - Extract a presentational `event-form-component` in `domain` (kept here because it binds api DTO types) so FC-3 and FC-6 share it. **If FC-3 lands first**, extract during FC-6 — leave FC-3 untouched until then.
  - Invitee section: chip-input → `EVENTS_SERVICE.addInvitee(...)`; `(x)` → `EVENTS_SERVICE.removeInvitee(...)`.
  - Host-only client guard: read `IsHost` from detail fetch on mount; non-host → navigate back to `/events/:id`.
  - `EventEditPagePom`, `event-update-flow.spec.ts`.
- **Acceptance test:** host edits date → save → redirect to detail with new date; add invitee → chip appears; remove invitee → chip disappears.
- **Eval doc:** `docs/evaluations/FI1-FT-019.md`.

### Task FC-7 (= FT-020) — Responsive viewport spec
- **Implements:** L2-051..L2-054.
- **Depends on:** FC-2, FC-4, FT-011 (already done).
- **Files:** `frontend/e2e/responsive.spec.ts`. Walks `/sign-in`, `/home`, `/events/{id}`, `/profile` at 360 / 768 / 1440 widths. Asserts:
  - bottom nav-bar visible <768, hidden ≥768
  - nav-rail visible ≥768, hidden <768
  - `document.documentElement.scrollWidth === clientWidth` at every width
  - primary action button is in the viewport without scroll at every width
- **Eval doc:** `docs/evaluations/FI1-FT-020.md`.

---

## 3. Execution sequence and parallelism

```
FC-1 (FT-014)  ──────────────────┐
FC-2 (FT-015)  ──┐               │
                 ├── FC-7 (FT-020)
FC-3 (FT-016)  ──┤               │
FC-4 (FT-017)  ──┤               │
                 │
FC-4 ── FC-5 (FT-018)
FC-3 + FC-4 ── FC-6 (FT-019)
```

Recommended order: **FC-1, FC-2, FC-3, FC-4, FC-5, FC-6, FC-7**.
FC-1 is independent and can land first or in parallel with FC-2.
FC-7 must be last because it asserts on the rendered FC-2 / FC-4 pages.

## 4. Definition of done for the project

- Every FT row in §1.2 reads "done" with a matching `docs/evaluations/FI1-FT-0NN.md` evaluation file.
- `ng build pull-up` reports 0 warnings, 0 errors.
- `npm test` (Vitest) and `npm run e2e` (Playwright) green from a fresh checkout.
- `dotnet test backend\PullUp.sln -c Release` green (already passing).
- All seven L2 ID groups listed in §1.3 are served end-to-end through the UI.
- No new `TODO`, `NotImplementedException`, `console.log`, or `bypassSecurityTrust*` introduced.

## 5. Out-of-scope follow-ups (record only)

Captured here so they are not lost; not part of this completion plan:

- Stand up the email/notification senders against a real provider (currently `LoggingEmailSender` / `LoggingNotificationSender`).
- Dependency-vulnerability scanning in CI (`dotnet list package --vulnerable`, `npm audit --omit=dev`) per L1-024.
- Audit-log retention enforcement / archival for the 90-day window per L1-021.
