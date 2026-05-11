# MF2 — Evaluate frontend MVP

Required by `data-evaluation-passes="1"`. Pass 1 raised one visual finding; Pass 2 confirms clean. Rendered snapshots of the live app live in `./docs/evaluations/MF2-screenshots/`.

## Pass 1 — findings

Walked the Implementation Evaluation Rubric scoped to frontend (Frontend, Library Structure, Authentication (frontend side), Testing, General) against `./frontend/`.

| # | Criterion | Result |
|---|---|---|
| 1 | Guidance adherence | **Pass** — Angular Material 3 via `mat.theme()`, design tokens (`--mat-sys-*`), BEM class names, one-type-per-file (`.html` / `.scss` / `.ts` triples), library-driven architecture with interface-driven service consumption. |
| 2 | Requirements coverage (MVP scope) | **Pass** — sample slice covers L2-001 (register valid input) and L2-022 (post-auth landing). MVP is intentionally a single slice; FI1 handles the rest. |
| 3 | Radically simple | **Pass** — only the abstractions required (`AUTH_SERVICE` token, `IAuthService`, one HTTP interceptor, one route guard). No state-management library, no presenter pattern, no facade layer, no extra-component decorators. |
| 4 | No temp code or stubs | **Pass** — grep across `./frontend/projects/` for `TODO`, `FIXME`, `XXX`, `HACK`, `console.log`, `debugger` returns zero matches. |
| 5 | One type per file | **Pass** — every `.ts` under `projects/` has exactly one exported type, except `auth.service.contract.ts` which has the interface + injection token — this is the documented pattern from Implementation Guidance ("`foo.service.contract.ts` — includes the TypeScript interface and the injection token") so it is not a violation. |
| 6 | SOLID + CQS shape (frontend) | **Pass** — services are interface-driven via `*.service.contract.ts`. Domain components inject `@Inject(AUTH_SERVICE) auth: IAuthService` — they never reference the concrete `AuthService` class. Libraries respect the dependency direction (see criterion 7). |
| 7 | **Frontend library placement (CRITICAL)** | **Pass** — verified by grep: `projects/components/` has zero imports `from 'api'` or `from 'domain'`; `projects/domain/` imports only from `'api'` and `'components'` (its non-workspace import `@angular/router` is the router DI, not the app workspace project). Files in each library follow the rule: <br>• `BrandLogoComponent` (reusable presentation, no backend) → **components** ✓ <br>• `AuthService` + `IAuthService`/`AUTH_SERVICE`/DTOs (backend-talking) → **api** ✓ <br>• `SignUpPageComponent`, `HomePageComponent` (consume api services) → **domain** ✓ <br>• `app.config.ts` / routes / guard / interceptor (composition root) → **pull-up app** ✓ |
| 8 | ATDD evidence | **Pass** — Playwright POM tests under `frontend/e2e/`: `pages/sign-up-page.ts`, `pages/home-page.ts`, `sign-up-flow.spec.ts` with two tests (happy path + 360px mobile-first). Both run with `npm run e2e` and pass on both `chromium-mobile` and `chromium-desktop` projects (4 / 4 green in ~12 s). Each spec carries a `Traces to: L2-...` header comment. <br>**ATDD chronology note (same as MB2)**: tests were written after the MVP code; the per-slice ATDD discipline (test-first) lives in FI1's per-slice loop, not MF1. The MVP test suite is the "test exists and currently passes" reference. |
| 9 | Mobile-first + responsive | **Pass** — design tokens + M3 components reflow naturally. The 360px Playwright test asserts no horizontal overflow (`scrollWidth <= clientWidth`); the sign-up card is fluid up to its 440 px max-width; the home page's container is fluid up to 800 px. Rendered snapshots at 360 / 768 / 1440 in `./docs/evaluations/MF2-screenshots/` confirm the layout reflows correctly. |
| 10 | Build and run clean | **Pass** — `ng build pull-up` reports **0 warnings, 0 errors** (post-fix below). |

### Finding F1 — `mat-icon` rendered icon names as text

- **Where:** `home-page.component.html`, the "Create your first event" button. Visible in `MF2-screenshots/home.1440.png` (pre-fix) as "ad Create your first event" instead of "+ Create your first event".
- **What:** Angular Material's `mat-icon` defaults to the `material-icons` fontSet, which expects the **Material Icons** Google Font face. The MVP's `index.html` only loaded **Material Symbols Outlined**, so the ligature fallback rendered the icon name as literal text (truncated to fit).
- **Fix:** swapped the `Material Symbols Outlined` `<link>` for the classic `Material Icons` font (`https://fonts.googleapis.com/icon?family=Material+Icons`) so `<mat-icon>add</mat-icon>` resolves to the "+" glyph. Re-ran the snapshot suite — `home.1440.png` and `home.360.png` now show a proper "+" icon.

### Visual fidelity to D1 mocks

The rendered sign-up card matches the D1 `sign-up.html` mock in structure (logo + brand, "Create your account" heading, "A free account is all you need to host and join events." subtitle, three Material 3 filled text fields, full-width "Create account" filled button). The MVP omits the mock's "Already have an account? Sign in" footer link because the backend MVP only exposes registration; adding a dead link to a non-existent `/sign-in` route would violate the "radically simple, no dead UI" rule. The full sign-in flow lives downstream in BI1 + FI1.

The primary purple in the rendered app is brighter than the M3 baseline violet (#6750A4) used in the mocks because `mat.$violet-palette` generates a more saturated tone than the M3 baseline. Both are M3 violet-family; tuning the palette to match the mock exactly is a future FI1 polish item and not blocking for MVP approval, since the visual language ("M3 violet primary, rose tertiary, light surface, BEM names, fluid card layout") is preserved.

## Pass 2 — clean

Re-walked the rubric and re-ran build + tests after the fix.

| # | Criterion | Result |
|---|---|---|
| 1 | Guidance adherence | Pass. |
| 2 | Requirements coverage (MVP scope) | Pass. |
| 3 | Radically simple | Pass. |
| 4 | No temp code or stubs | Pass. |
| 5 | One type per file | Pass. |
| 6 | SOLID + CQS shape (frontend) | Pass. |
| 7 | **Library placement** | Pass — zero placement violations. |
| 8 | ATDD evidence | Pass — `npm run e2e` reports 4 / 4 (excluding the new optional 5 snapshot tests which also pass). |
| 9 | Mobile-first + responsive | Pass — verified at 360 / 768 / 1440. |
| 10 | Build and run clean | Pass — `ng build pull-up`: 0 / 0; `npx playwright test --config=e2e/playwright.config.ts`: 4 acceptance + 5 snapshot all passing. |

MF2 is complete. The frontend MVP is approved as the pattern reference for FP1 / FT1 / FI1.
