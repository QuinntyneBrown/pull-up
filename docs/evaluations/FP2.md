# FP2 — Evaluate frontend plan

Required by `data-evaluation-passes="1"`. Pass 1 raised one minor coverage finding; Pass 2 clean.

## Pass 1 — findings

Walked the FP2 explicit-checks list against `./docs/plans/frontend.md`.

| # | Check | Result |
|---|---|---|
| 1 | Three libraries (`api`, `components`, `domain`) + app with correct dependency direction | **Pass** — §1 of the plan names the four projects and the dependency direction in three bullets: `components` → nothing from `api`/`domain`; `domain` → `api`+`components`; `pull-up` app → all three. |
| 2 | Every planned service has a `*.service.contract.ts` | **Pass** — §3 names three feature folders under `api/src/lib/` (auth, profile, events). Each backend-talking service is paired with its `.service.contract.ts` (already established for `AuthService` in MF1; the plan adds the same shape for `ProfileService` and `EventsService`). |
| 3 | Angular Material 3 components specified | **Pass** — §7 references the existing MF1 `mat.theme(...)` setup with violet primary + rose tertiary. Every page in §5 lists which Material components it composes. |
| 4 | Design tokens specified | **Pass** — §7 documents the existing `--mat-sys-*` token surface and adds `--pu-space-*` tokens (4/8/12/16/24/32/48/64 px) for spacing parity with the D1 mocks. |
| 5 | BEM naming assumed | **Pass** — §1 explicitly states "the MF1 reference shape (BEM + one-type-per-file + Material 3 + …) is taken as given — no plan item re-litigates it." The MF1 SCSS already uses BEM (verified in `BI1-MF2.md`). |
| 6 | One-type-per-file with `.html`/`.scss`/`.ts` split assumed | **Pass** — same MF1-given block in §1; plus the MF1 component triple pattern is enforced in §4 / §5 component descriptions. |
| 7 | Playwright POM tests planned for important flows | **Pass** — §9 inventory has 10 spec files: sign-up (exists), sign-in, password-reset, event-create, event-rsvp, event-update, event-cancel, profile-edit, delete-account, responsive. Each names its POMs and L2 IDs. |
| 8 | Local username/password auth flow planned | **Pass** — §8 walks the full local-credential frontend flow: storage in localStorage via `auth-storage.ts`, `Authorization: Bearer` injection via `authJwtInterceptor`, 401 → transparent refresh + retry (L2-006), sign-out clears tokens, password-reset both sides, account-delete clears + redirect, email-change request + confirm. No third-party identity provider is introduced. |
| 9 | Mock-to-screen mapping exhaustive | **Pass** — every D1 product mock has a domain page: sign-in / sign-up / password-reset (auth band), home / event-detail / event-create (event band), profile (account band), empty-state and error-state are reusable presentation components consumed by the home page. The `index.html` mock-gallery file is a discovery aid, not a product screen, so no page is bound to it (consistent with how D2 evaluated it). |

### Library placement (CRITICAL — explicit rubric in BT-018)

Walked every artifact named in the plan against the placement rule:

| Type of artifact | Library | Examples in plan |
|---|---|---|
| Reusable presentation component, no api dependency | `components` | `BrandLogoComponent` (exists), `AppBarComponent`, `BottomNavBarComponent`, `NavRailComponent`, `EventCardComponent`, `EmptyStateComponent`, `ErrorStateComponent`, `FilterStripComponent`, `SegmentedButtonComponent`, `RsvpAvatarStackComponent`, `LoadingSkeletonComponent` — 11 components. None inject services. |
| Model / DTO / backend-facing service + its contract | `api` | All 8 DTOs + 3 services + 3 contracts (`AuthService`, `ProfileService`, `EventsService`). The `auth-storage.ts` refactor is also in `api` since it lives behind `AuthService`. |
| Component that consumes an api service (directly or via the interceptor stack) | `domain` | All 12 page components from §5. Every one explicitly lists which `AUTH_SERVICE` / `PROFILE_SERVICE` / `EVENTS_SERVICE` token it injects. |
| Composition root (routes, providers, guards, interceptors) | `pull-up` app | `app.config.ts`, `app.routes.ts`, `auth.guard.ts`, `auth-jwt.interceptor.ts`, `error.interceptor.ts`. |

The plan explicitly addresses one tricky case (`EventFormComponent`): a form binds to `api` DTOs by type, so the plan places it in `domain` — preserving the "components imports nothing from api" rule. Called out in §5's note rather than assumed silently.

### Finding F1 — L2-067 (dep hygiene CI scan) not acknowledged

- **Where:** `docs/plans/frontend.md` §11 cross-cutting non-functionals.
- **What:** L2-067 ("`npm audit --omit=dev --audit-level=high` runs in CI; high/critical fails the build") is a frontend-side CI obligation. The plan never mentioned it. The backend plan punted to DP1 explicitly; the frontend plan should do the same so a reader can confirm coverage at a glance.
- **Severity:** Documentation completeness. Doesn't change any code, but the rubric requires every relevant L2 to be reflected.
- **Fix:** added a row to §11 naming L2-067, pointing the actual CI step to DP1, and recording the frontend's running obligation (keep `package.json` clean enough that the audit stays green).

## Pass 2 — clean

Re-walked the 9 explicit checks against the updated plan.

| # | Check | Result |
|---|---|---|
| 1 | Three libraries + app + dependency direction | Pass. |
| 2 | Every planned service has `*.service.contract.ts` | Pass. |
| 3 | Angular Material 3 components | Pass. |
| 4 | Design tokens | Pass. |
| 5 | BEM | Pass. |
| 6 | One-type-per-file / per-file split | Pass. |
| 7 | Playwright POM inventory | Pass. |
| 8 | Local username/password auth flow | Pass. |
| 9 | Mock coverage exhaustive | Pass. |
| — | Library placement (CRITICAL) | Pass — zero violations across the inventories. |
| — | L2 coverage (incl. L2-067) | Pass — every L2 has a frontend home or is intentionally outside scope. |

FP2 is complete. The frontend plan is approved; FT1 may break it into vertically-sliced frontend tasks.
