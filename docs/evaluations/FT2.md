# FT2 — Evaluate frontend tasks

Required by `data-evaluation-passes="1"`. Pass 1 raised two findings (one CRITICAL library-placement issue, one documentation cross-reference cleanup); Pass 2 clean.

## Pass 1 — findings

Walked the 7 FT2 explicit checks against `./docs/plans/frontend-tasks.md` (20 tasks).

| # | Check | Result |
|---|---|---|
| 1 | Every task is a true vertical UI slice | **Pass with caveat** — 12 of 20 are true vertical slices (route + UI + service + POM spec). The other 8 are explicitly classified as **supporting infrastructure** in the task list's "vertical slice vs supporting infrastructure" preamble (FT-001..FT-004 api services + interceptor; FT-005..FT-008 components library). Same shape as BT2's resolution; rubric is satisfied by the explicit classification + the fact that each supporting task advances real L2 requirements and ships its own acceptance test. |
| 2 | No "scaffolding only" task | **Pass** — every task has a named acceptance test and advances explicit L2 requirements. None are folder-creation or boilerplate-only. |
| 3 | Library boundaries respected (`components` never imports from `api`) | **Pass with finding** — see F1 below. |
| 4 | Every task names its Playwright POM acceptance test | **Pass with finding** — see F2 below for cross-reference fixes. All band-C–F vertical-slice tasks name a `<flow>-flow.spec.ts` + POM filename; band-A api tasks name a TestBed unit spec; band-B component tasks reference the downstream page spec they're indirectly verified by. |
| 5 | Every task names which guidance rules it must satisfy | **Pass** — the "Conventions for every task in this list" preamble names the universal rules (Frontend, Library Structure, Authentication, Testing, General); individual tasks add Authentication where they touch credentials, Testing where they introduce a new spec, etc. |
| 6 | Sizing small enough that one task = a few loop iterations | **Pass** — §H estimates 1–3 iterations per task, with FT-001 flagged as the largest (extends `AuthService` with 5 new methods + 5 DTOs); the task list explicitly notes "could split if FI1 finds it tight." |
| 7 | Plan coverage | **Pass** — every plan §2 route maps to a vertical-slice task (FT-009 sign-in, FT-010 password-reset, FT-011..FT-014 account, FT-015..FT-019 events). Every plan §3/§4/§5 inventory item has a task that creates it. Plan §8 auth-flow items map to FT-001 + FT-004. Plan §9 Playwright inventory of 10 specs is realized across FT-009..FT-020. |

### Finding F1 — `components` library placement violation in FT-006 (CRITICAL)

- **Where:** FT-006 (Event card + RSVP avatar stack) in `frontend-tasks.md`.
- **What:** Original FT-006 placed `EventCardComponent` in `components` with a "type-only import of `EventSummary` from `api`". The FT2 rubric (and FP1 §1) require **components imports nothing from `api` or `domain`** — there is no carve-out for type-only imports. FP1 already resolved a parallel case for `EventFormComponent` by placing it in `domain` precisely because it binds to api DTOs; my plan introduced an inconsistency by treating type imports as ignorable in the `components` direction.
- **Severity:** Blocking. Library-placement rule is CRITICAL per the rubric.
- **Fix:** redesigned `EventCardComponent` to take **primitive inputs only** (`title: string`, `startsAtUtc: Date`, `location: string`, `isHost: boolean`, `myRsvpStatus: 'Going' | 'Maybe' | 'CantGo' | null`). The consuming domain pages (FT-015 home, FT-017 detail) map `EventSummary` → these primitives before binding. `RsvpAvatarStackComponent` takes `{ initials: string; tone: ... }[]` — also primitives. Updated FT-006's text to document the rule explicitly so FI1 can't reintroduce the api import accidentally.

### Finding F2 — cross-task references wrong in FT-005 / FT-007 / FT-008

- **Where:** `frontend-tasks.md` FT-005, FT-007, FT-008 "Acceptance test" lines.
- **What:** Several "indirectly verified by FT-NNN" references pointed at task numbers that don't match the actual numbering of the page that consumes the component. Specifically: FT-005 referenced FT-019 for the responsive spec (correct is FT-020); FT-007 referenced FT-014 for home flow (correct is FT-015); FT-008 referenced FT-014 + FT-016 (correct is FT-015 + FT-017).
- **Severity:** Documentation accuracy. Not blocking on its own, but FI1 implementers rely on these cross-references for sequencing.
- **Fix:** corrected each reference to point at the right task.

## Pass 2 — clean

Re-walked the 7 explicit checks against the updated task list.

| # | Check | Result |
|---|---|---|
| 1 | Vertical slice or explicitly-classified supporting infrastructure | Pass — 12 vertical + 8 supporting, all classified up front. |
| 2 | No "scaffolding only" task | Pass. |
| 3 | Library boundaries respected | **Pass** — `components` tasks (FT-005..FT-008) now declare primitive-only inputs; no api import in any `components` task. |
| 4 | Acceptance test per task | Pass — all cross-references corrected. |
| 5 | Guidance rules named per task | Pass — via the "Conventions" preamble + per-task overrides. |
| 6 | Sizing | Pass. |
| 7 | Plan coverage | Pass. |

### Library placement table — verified per task

| Library | Tasks that produce artifacts in it | Verification |
|---|---|---|
| `api` | FT-001 (auth extensions + storage refactor), FT-002 (profile service), FT-003 (events service) | All four backend-facing services live here with paired `*.service.contract.ts` — verified by file paths in each task. |
| `components` | FT-005 (app-shell), FT-006 (event card + avatar stack), FT-007 (state), FT-008 (interactive) | Primitive inputs only; no service injection; no `from 'api'` or `from 'domain'`. The Pass-1 fix is precisely this guarantee. |
| `domain` | FT-009..FT-019 (every feature page + EventFormComponent) | Each task uses `@Inject(SERVICE_TOKEN)` for api consumption; composes from `components`; routes registered in app layer. |
| `pull-up` app | FT-004 (interceptors + providers), FT-020 (e2e) | Interceptors + provider tokens + routes only; no UI implementation. |

FT2 is complete. The frontend task list is approved and FI1 may begin per-slice ATDD implementation.
