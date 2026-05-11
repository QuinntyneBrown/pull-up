# Frontend implementation plan — Pull Up Angular

This plan translates the 24 L1 / 67 L2 requirements, the 10 D1 mocks, and the now-implemented backend API surface into a concrete Angular 21 implementation grounded in the MF1 MVP's patterns. Every section cites the L2 IDs it advances and the Implementation-Guidance rules it must satisfy. The MF1 reference shape (workspace + three libraries + standalone components + Angular Material 3 + interface-driven services + BEM + one-type-per-file + Playwright POM) is taken as given — no plan item re-litigates it.

The backend API the frontend integrates against is the one delivered by BI1: 23 endpoints across `UsersController`, `AuthController`, `EventsController`, `HealthController`. The auth scheme is HS256 JWT bearer; refresh tokens are 256-bit opaque values stored server-side as HMAC hashes.

## 1. Workspace layout (the MF1 shape is final)

```
frontend/
  projects/
    api/                 backend-talking services + DTOs (no UI, no DOM)
    components/          reusable presentation components (no api dependency)
    domain/              api-consuming feature components (depends on api + components)
    pull-up/             the application (depends on api + components + domain)
  e2e/                   Playwright POM tests
```

Path aliases in `tsconfig.json` point each library at its `public-api.ts` so the app consumes libraries from source without a separate `ng build api` step.

**Dependency direction (enforced by import grep in FT2 / FI1 evals):**

- `components` → nothing from `api` or `domain`.
- `domain` → `api`, `components`, `@angular/*`, no `app`.
- `pull-up` → all three libraries.

## 2. Routing (single SPA, lazy-loaded routes)

| Path | Auth | Component | Library | L2 refs |
|---|---|---|---|---|
| `/` | n/a | redirect → `/home` (authed) or `/sign-in` (anon) | pull-up app | — |
| `/sign-in` | anon | `SignInPageComponent` | domain | L2-004, L2-005 |
| `/sign-up` | anon | `SignUpPageComponent` (exists from MF1) | domain | L2-001..L2-003 |
| `/password-reset` | anon | `RequestPasswordResetPageComponent` | domain | L2-008 |
| `/password-reset/confirm` | anon | `CompletePasswordResetPageComponent` | domain | L2-003, L2-009 |
| `/home` | bearer | `HomePageComponent` (extend MF1's welcome card to full event list) | domain | L2-022, L2-024, L2-025, L2-062, L2-063 |
| `/events/new` | bearer | `EventCreatePageComponent` | domain | L2-018..L2-021, L2-031..L2-033 |
| `/events/:id` | bearer | `EventDetailPageComponent` | domain | L2-023, L2-026..L2-030, L2-034..L2-036 |
| `/events/:id/edit` | bearer | `EventEditPageComponent` (reuses the create form) | domain | L2-026..L2-028 |
| `/profile` | bearer | `ProfilePageComponent` | domain | L2-011..L2-013, L2-016 |
| `/profile/delete` | bearer | `DeleteAccountPageComponent` | domain | L2-014, L2-015 |
| `/email-change/confirm` | bearer | `ConfirmEmailChangePageComponent` | domain | L2-013 |

All authenticated routes are guarded by the existing `authGuard`. All page components are loaded via `loadComponent: () => import('domain').then(m => m.X)` for code-splitting.

## 3. `api` library inventory

One sub-folder per backend feature. Each backend-talking service exposes an interface + injection token in `*.service.contract.ts`, paired with a concrete `*.service.ts`. The MVP's `AuthService` stays; the rest are new.

| Folder | Files | DTOs / contracts |
|---|---|---|
| `api/src/lib/auth/` (exists) | `auth.service.{ts,contract.ts}`, `register-user-request.ts`, `register-user-response.ts`, `current-user.ts` | extend `IAuthService` with `signIn`, `requestPasswordReset`, `completePasswordReset`, `refresh`, `signOut`. Add DTOs: `SignInRequest`, `SignInResponse`, `RequestPasswordResetRequest`, `CompletePasswordResetRequest`, `RefreshAccessTokenRequest`, `RefreshAccessTokenResponse`, `SignOutRequest`. |
| `api/src/lib/profile/` | `profile.service.{ts,contract.ts}`, `update-profile-request.ts`, `request-email-change-request.ts`, `confirm-email-change-request.ts`, `delete-account-request.ts`, `notification-preferences.ts`, `update-notification-preferences-request.ts` | `IProfileService.updateProfile`, `requestEmailChange`, `confirmEmailChange`, `deleteAccount`, `getNotificationPreferences`, `updateNotificationPreferences`. |
| `api/src/lib/events/` | `events.service.{ts,contract.ts}`, `event-summary.ts`, `list-my-events-response.ts`, `event-detail.ts`, `host-summary.ts`, `guest-summary.ts`, `create-event-request.ts`, `create-event-response.ts`, `update-event-request.ts`, `add-invitee-request.ts`, `set-rsvp-request.ts`, `rsvp-status.ts` (string union) | `IEventsService.list`, `get`, `create`, `update`, `cancel`, `addInvitee`, `removeInvitee`, `setRsvp`. |

**Cross-cutting in `api`:**

- `api-base-url.token.ts` (exists) — `API_BASE_URL` injection token.
- `auth-storage.ts` (refactor out of `AuthService`) — single-responsibility wrapper around `localStorage` for the JWT + refresh-token persistence used by the interceptor + guard. Adding it now keeps the auth service from carrying its own storage policy.

## 4. `components` library inventory

Pure presentation components — no service injections, no router-link, no api imports. Imported by domain feature pages.

| Component | Purpose | L2 refs |
|---|---|---|
| `BrandLogoComponent` (exists) | The "P + Pull Up" brand mark used across auth + app-bar. | — |
| `AppBarComponent` | Material 3 top app bar: leading icon, title, trailing avatar slot. Inputs: `title`, `showBack`. | L2-011 |
| `BottomNavBarComponent` | Mobile bottom nav (Events / People / Profile). Active route via input. Hidden by CSS on ≥768px. | L2-052 |
| `NavRailComponent` | Tablet / desktop ≥768px left nav rail. | L2-052 |
| `EventCardComponent` | Date block + title + meta line + RSVP-state chip. Pure inputs: `event: EventSummary`, `myRsvpStatus`. | L2-022, L2-023 |
| `EmptyStateComponent` | Centered icon + title + supporting text + primary CTA slot. | L2-062 |
| `ErrorStateComponent` | Same shape as EmptyState but error-themed; retry CTA. | L2-063 |
| `FilterStripComponent` | Horizontally-scrolling chip strip used on Home. Inputs: `chips`, `selectedKey`; output: `chipChange`. | L2-025 |
| `SegmentedButtonComponent` | M3 segmented control used by event-detail RSVP picker. | L2-034 |
| `RsvpAvatarStackComponent` | Overlapping avatar list for guest summary. | L2-023 |
| `LoadingSkeletonComponent` | Material 3 skeleton placeholder for loading rows. | L2-022 |

## 5. `domain` library inventory

Feature pages that compose `components` widgets and depend on `api` services through their contracts. Every page injects services via the `*.service.contract.ts` token, never the concrete class.

| Page | Routes | Consumes (api) | Composes (components) |
|---|---|---|---|
| `SignInPageComponent` | `/sign-in` | `AUTH_SERVICE` | `BrandLogoComponent` |
| `SignUpPageComponent` (exists) | `/sign-up` | `AUTH_SERVICE` | `BrandLogoComponent` |
| `RequestPasswordResetPageComponent` | `/password-reset` | `AUTH_SERVICE` | `BrandLogoComponent` |
| `CompletePasswordResetPageComponent` | `/password-reset/confirm` | `AUTH_SERVICE` | `BrandLogoComponent` |
| `HomePageComponent` (replace MF1's welcome) | `/home` | `EVENTS_SERVICE`, `AUTH_SERVICE` | `AppBarComponent`, `BottomNavBarComponent`, `NavRailComponent`, `FilterStripComponent`, `EventCardComponent`, `EmptyStateComponent`, `ErrorStateComponent`, `LoadingSkeletonComponent` |
| `EventCreatePageComponent` | `/events/new` | `EVENTS_SERVICE` | `AppBarComponent`, M3 form components |
| `EventDetailPageComponent` | `/events/:id` | `EVENTS_SERVICE`, `AUTH_SERVICE` | `AppBarComponent`, `SegmentedButtonComponent`, `RsvpAvatarStackComponent`, `BottomNavBarComponent`, `NavRailComponent` |
| `EventEditPageComponent` | `/events/:id/edit` | `EVENTS_SERVICE` | reuses `EventCreatePageComponent` form via a presentational `EventFormComponent` (shared in domain, not components, because it binds to api types) |
| `ProfilePageComponent` | `/profile` | `AUTH_SERVICE`, `PROFILE_SERVICE` | `AppBarComponent`, `BottomNavBarComponent`, `NavRailComponent` |
| `DeleteAccountPageComponent` | `/profile/delete` | `PROFILE_SERVICE` | `AppBarComponent` |
| `ConfirmEmailChangePageComponent` | `/email-change/confirm` | `PROFILE_SERVICE` | `BrandLogoComponent` |

## 6. App layer (`pull-up`)

- `app.config.ts` (exists) — wire `API_BASE_URL`, `AUTH_SERVICE`, `PROFILE_SERVICE`, `EVENTS_SERVICE`, http interceptors, animations, routing.
- `app.routes.ts` (exists, extend) — declare every route from §2.
- `auth.guard.ts` (exists) — no change.
- `auth-jwt.interceptor.ts` (exists) — extend to call `AuthService.tryRefresh()` on a 401 response **once**, then retry. This implements L2-006's transparent refresh.
- `error.interceptor.ts` (new) — single place to surface backend `ProblemDetails` to a `MatSnackBar` for non-form errors.
- `index.html` (exists) — fonts already loaded.
- `styles.scss` (exists) — Material 3 theme already in place. Add design tokens for spacing scale and breakpoint variables if not already.

## 7. Material 3 design tokens

The MF1 styles bundle already establishes the M3 theme via `mat.theme(...)` with violet primary + rose tertiary. The plan adds explicit `--pu-space-*` tokens in `styles.scss` for spacing parity with the D1 mocks (4 / 8 / 12 / 16 / 24 / 32 / 48 / 64 px). The mocks' tighter purple — closer to the M3 baseline #6750A4 — is the intended future tuning; the rendered theme is already in the M3 violet family. FI1 may keep `mat.$violet-palette` or switch to a hand-rolled palette generated from `#6750A4`; both satisfy the L2-016-implied "M3 design system".

## 8. Authentication flow (frontend side)

- **Sign in / sign up** — store the access token + refresh token in `localStorage` via `auth-storage.ts`. The `AuthService` emits `accessToken$` and `currentUser$` as `BehaviorSubject` observables (MF1 shape stays).
- **Authorized request** — `authJwtInterceptor` attaches `Authorization: Bearer <token>` (MF1, no change).
- **401 recovery** (L2-006) — `authJwtInterceptor` on 401 calls `AuthService.refresh(refreshToken)`. On success it retries the original request once; on failure it clears tokens + redirects to `/sign-in`.
- **Sign out** — call `POST /api/auth/sign-out` with the current refresh token, then clear tokens + redirect.
- **Password reset** — `RequestPasswordReset` page submits email and always shows the same "If an account exists…" UI per L2-008. `CompletePasswordReset` page reads the token from `?token=...` query string.
- **Account delete** — re-typed password modal, on 204 clears tokens + redirects to `/sign-up`.
- **Email change** — `ProfilePage` opens a confirm dialog requiring current password; on success shows "Verification email sent". `ConfirmEmailChangePage` reads `?token=` and posts confirmation.

## 9. Playwright POM test inventory (`frontend/e2e/`)

The MF1 sign-up flow test stays. Each FI1 slice will introduce a POM + spec under the same pattern, mocking the backend via `page.route()` so e2e runs without a running .NET API.

| Spec | POMs | L2 refs |
|---|---|---|
| `sign-up-flow.spec.ts` (exists) | `SignUpPagePom`, `HomePagePom` | L2-001 |
| `sign-in-flow.spec.ts` | `SignInPagePom`, `HomePagePom` | L2-004, L2-005 |
| `password-reset-flow.spec.ts` | `RequestPasswordResetPagePom`, `CompletePasswordResetPagePom` | L2-008, L2-009 |
| `event-create-flow.spec.ts` | `HomePagePom`, `EventCreatePagePom`, `EventDetailPagePom` | L2-018, L2-022 |
| `event-rsvp-flow.spec.ts` | `EventDetailPagePom` | L2-034, L2-035 |
| `event-update-flow.spec.ts` | `EventDetailPagePom`, `EventEditPagePom` | L2-026 |
| `event-cancel-flow.spec.ts` | `EventDetailPagePom` | L2-029 |
| `profile-edit-flow.spec.ts` | `ProfilePagePom` | L2-012, L2-013, L2-016 |
| `delete-account-flow.spec.ts` | `DeleteAccountPagePom` | L2-014 |
| `responsive.spec.ts` (extend the existing 360px assertion) | reuses other POMs | L2-051..L2-054 |

## 10. Deferred-integration list

Same shape as backend BP1 §9. Frontend deferrals:

- **In-app notifications surface** — the BI1 dispatcher writes to `LoggingNotificationSender` (a no-op). A `notifications` API + a frontend bell-icon panel are explicitly **out of scope** for FI1; the dispatcher already gates by preference, so when the surface is added later it consumes the existing data shape.
- **External identity providers** (Google / Apple sign-in) — out of scope per L1-001 / L2-001 ("local users only" in MVP scope).
- **Push notifications / service worker** — out of scope; the in-app surface is the future entry point.
- **Internationalization (i18n)** — out of scope. Copy is English-only; `@angular/localize` is not added to the workspace.

Each deferral is named at the boundary it sits behind so a future swap-in is mechanical.

## 11. Cross-cutting non-functionals

| L2 area | Plan item |
|---|---|
| Responsive (L2-051..L2-054) | M3 components reflow naturally; per-page SCSS uses the spacing tokens + 768 / 992 / 1200 media queries; `BottomNavBarComponent` hides at ≥768, `NavRailComponent` shows. Playwright responsive spec asserts no horizontal overflow at 360 / 768 / 1440. |
| Keyboard navigation (L2-055) | Every interactive element is a native button / link / form field, focus styles unaltered from M3 defaults. |
| Color contrast (L2-056) | Verified against M3 token contrasts by visual inspection during FI1 evals; no per-page overrides darken text below the M3 default. |
| Form labels (L2-057) | Every Material form field uses `<mat-label>` — Angular Material associates it programmatically. |
| Output encoding / XSS (L2-066) | Angular's default interpolation is safe; the plan **forbids any use of `bypassSecurityTrust*`** on user content — captured in the FI1 per-slice rubric. |
| Performance (L2-058) | `HomePageComponent` lazy-loads the list, displays skeleton placeholders, uses `OnPush` change detection. Image / icon usage stays minimal. |

## 12. What this plan does **not** introduce

- A state-management library (NgRx, Akita, etc.). Component-level `signal` + `BehaviorSubject` in services is enough for MVP scope.
- A facade layer between components and services. Components inject the contract token directly.
- A separate Angular module per feature (the workspace is standalone-components-only).
- A presenter / view-model pattern. Pages own their reactive state via `signal`s.
- An i18n / a11y testing framework. WCAG checks are visual + manual during FI1 evals.
- A separate "shared" library between `components` and `domain`. If a piece of presentation logic doesn't fit in either, it goes in the page that uses it (single-use rule).

## 13. Mapping table — every backend endpoint → frontend consumer

| Endpoint | Consumer (domain page) | Service method |
|---|---|---|
| `POST /api/users` | SignUpPage | `AuthService.register` |
| `GET /api/users/me` | (any authed page) | `AuthService.loadCurrentUser` |
| `PUT /api/users/me/profile` | ProfilePage | `ProfileService.updateProfile` |
| `POST /api/users/me/email-change` | ProfilePage | `ProfileService.requestEmailChange` |
| `POST /api/users/me/email-change/confirm` | ConfirmEmailChangePage | `ProfileService.confirmEmailChange` |
| `DELETE /api/users/me` | DeleteAccountPage | `ProfileService.deleteAccount` |
| `GET /api/users/me/notification-preferences` | ProfilePage | `ProfileService.getNotificationPreferences` |
| `PUT /api/users/me/notification-preferences` | ProfilePage | `ProfileService.updateNotificationPreferences` |
| `POST /api/auth/sign-in` | SignInPage | `AuthService.signIn` |
| `POST /api/auth/refresh` | (interceptor) | `AuthService.refresh` |
| `POST /api/auth/sign-out` | ProfilePage | `AuthService.signOut` |
| `POST /api/auth/password-reset` | RequestPasswordResetPage | `AuthService.requestPasswordReset` |
| `POST /api/auth/password-reset/confirm` | CompletePasswordResetPage | `AuthService.completePasswordReset` |
| `GET /api/events?scope=...` | HomePage | `EventsService.list` |
| `POST /api/events` | EventCreatePage | `EventsService.create` |
| `GET /api/events/{id}` | EventDetailPage | `EventsService.get` |
| `PUT /api/events/{id}` | EventEditPage | `EventsService.update` |
| `POST /api/events/{id}/cancel` | EventDetailPage | `EventsService.cancel` |
| `POST /api/events/{id}/invitees` | EventEditPage | `EventsService.addInvitee` |
| `DELETE /api/events/{id}/invitees/{invId}` | EventEditPage | `EventsService.removeInvitee` |
| `PUT /api/events/{id}/rsvp` | EventDetailPage | `EventsService.setRsvp` |
| `GET /health/live` | (none — ops) | — |
| `GET /health/ready` | (none — ops) | — |

Every endpoint is consumed; no orphan API surface; no consumer references an endpoint that doesn't exist.

FT1 will break this plan into vertically-sliced frontend tasks; FI1 will implement each slice ATDD-first against the Playwright POM e2e suite already established in MF1.
