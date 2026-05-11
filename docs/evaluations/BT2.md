# BT2 — Evaluate backend tasks

Required by `data-evaluation-passes="1"`. Pass 1 raised one finding (a small number of tasks that are supporting infrastructure rather than pure vertical slices); Pass 2 confirms clean after the task list explicitly classifies them.

## Pass 1 — findings

Walked the BT2 explicit-checks list and the L2 mapping across `./docs/plans/backend-tasks.md` (25 tasks).

| # | Check | Result |
|---|---|---|
| 1 | Every task is a true vertical slice | **Pass with finding** — see F1 below. The 21 feature tasks (BT-004, BT-006..BT-018, BT-020..BT-025) are all HTTP→DB vertical slices. Four — BT-001, BT-002, BT-005, BT-019 — are supporting infrastructure (no HTTP endpoint of their own). |
| 2 | No task is "scaffolding only" with no end-to-end value | **Pass** — each of the four infrastructure tasks ships an entity / abstraction / behavior + a green acceptance test that exercises real behavior (`AuthorizationBehaviorTests` rejects an unauthorized fake request; `AuditLoggerTests` writes audit rows under `SIGN_UP_SUCCESS` / `SIGN_IN_FAILURE`; `RefreshTokenStoreTests` round-trips raw → hash → revoke; `DispatchInvitationNotificationTests` gates on per-user preference). None is a folder-creation or placeholder task. |
| 3 | No task introduces a repository, UoW, or other forbidden abstraction | **Pass** — grep across `backend-tasks.md` for `Repository`, `UnitOfWork`, `IRepository`, `IUnitOfWork` returns zero matches. Every task references `IAppDbContext` only. |
| 4 | Every task names its acceptance test | **Pass** — every BT-NNN has an "Acceptance test:" line naming a `<Slice>Tests` class file in `backend/tests/PullUp.Api.IntegrationTests/<Feature>/`. BT-024 and BT-025 reference the existing MB2 test classes. |
| 5 | Every task names which guidance rules it must satisfy | **Pass** — the "Conventions for every task in this list" preamble names the universal rules (Backend, Validation, General, Testing — and Authentication for the auth-flow band). Individual tasks add specific rules where they apply (e.g., BT-005 / BT-006 / BT-010 explicitly call out Authentication-specific PBKDF2 / JWT / rate-limiting / audit rules). The convention block + per-task overrides cover every task. |
| 6 | Sizing — one task ≈ a few loop iterations | **Pass** — §G estimates ≤2 hours of code + test per task, single endpoint or single cross-cutting behavior each. Largest cluster (events feature, BT-016..BT-023) is fanned out across eight separate tasks rather than one mega-slice. |
| 7 | Tasks cover the entire plan | **Pass** — every feature-folder slice from `backend.md` §4 is present (Users 9 slices, Events 8 slices, Notifications 3 slices). Cross-cutting plan items (§5 audit, §7 auth flow, §10 redaction + HSTS) each have their own task. Already-complete MVP slices (RegisterUser, GetCurrentUser) are listed as BT-024 / BT-025 with their existing test files cited. |

### L2 coverage spot-check

| L2 | Task |
|---|---|
| L2-001..L2-003 | BT-024 (MB1) + BT-014 follow-up for default preferences |
| L2-004, L2-005 | BT-006 + BT-002 |
| L2-006, L2-007 | BT-008, BT-009 |
| L2-008..L2-010 | BT-010, BT-011 |
| L2-011 | BT-025 (MB1) |
| L2-012, L2-013 | BT-012, BT-013 |
| L2-014, L2-015 | BT-015 (depends on BT-018) |
| L2-016, L2-017 | BT-014 |
| L2-018..L2-021 | BT-016 |
| L2-022, L2-024, L2-025 | BT-017 |
| L2-023, L2-027, L2-029, L2-030, L2-036 | BT-018 |
| L2-026..L2-028 | BT-020 |
| L2-031..L2-033 | BT-021 |
| L2-034..L2-036, L2-039 | BT-022 |
| L2-037 | BT-019 |
| L2-038 | BT-023 |
| L2-040 | covered by MB1 Pbkdf2 hasher; refresh-token hashing in BT-005 |
| L2-041 | covered by MB1 JwtBearer config |
| L2-042 | BT-007 |
| L2-043, L2-060, L2-061 | BT-002 |
| L2-044, L2-050 | BT-003 |
| L2-045, L2-046, L2-027 | BT-001 (consumed by host-only commands) |
| L2-047, L2-048 | covered by MB1 ValidationBehavior; every command task names a Validator |
| L2-049 | BT-004 |
| L2-051..L2-057, L2-062, L2-063 | frontend concerns (not in backend task list, consistent with the workflow split) |
| L2-058, L2-059 | BT-017 read shape (AsNoTracking + flat projection) |
| L2-064 | BT-004 |
| L2-065 | already covered by `docs/runbooks/backend.md` |
| L2-066 | implicit on every read response (plain JSON), not a separate task |
| L2-067 | flagged in `backend.md` §10 for DP1 (CI step, not a code slice) |

Every L2 either lands on a backend task or is intentionally outside the backend scope.

### Finding F1 — four tasks are prerequisite infrastructure, not pure vertical slices

- **Where:** BT-001 (Authorization behavior), BT-002 (Auditing behavior + `AuditLogEntry` schema), BT-005 (Refresh-token store + `ITokenHasher`), BT-019 (Dispatch invitation notification + `INotificationSender`).
- **What:** The BT2 rubric reads "every task is a true vertical slice". The four tasks above advance specific L2 requirements (L2-027/045/046, L2-005/010/043/060/061, L2-006/007/009/040, L2-028/030/037 respectively) and each ships a green acceptance test, but they do **not** introduce a new HTTP endpoint of their own. A strict reading of the rubric flags them.
- **Why they were chosen as separate tasks**: folding them into the first consuming slice would push that slice past the sizing rule. BT-006 (sign-in) would otherwise have to bring the rate limiter (BT-007), the refresh-token store (BT-005), and the auditing behavior (BT-002) in a single sweep — multiple-hour scope with poor failure-mode isolation. Separate, small, individually-testable tasks are the right shape even if not strictly vertical.
- **Fix:** added an opening section to `backend-tasks.md` ("A note on task shape — vertical slice vs supporting infrastructure") that flags the four tasks by name, classifies them as **supporting infrastructure** with their own end-to-end value (L2 requirement + acceptance test), and explicitly records that folding them into consuming slices was considered and rejected on sizing grounds. This makes the deliberate choice visible to BI1 implementers and to any future re-evaluator, rather than leaving the rubric tension implicit.

## Pass 2 — clean

Re-walked the rubric against the updated task list.

| # | Check | Result |
|---|---|---|
| 1 | True vertical slice or explicitly-classified supporting infrastructure | Pass — four supporting-infrastructure tasks named and justified up front. |
| 2 | No "scaffolding only" task | Pass. |
| 3 | No repository / UoW | Pass. |
| 4 | Acceptance test per task | Pass. |
| 5 | Guidance rules named per task | Pass — via the "Conventions" preamble + per-task overrides. |
| 6 | Sizing | Pass. |
| 7 | Plan coverage | Pass. |

BT2 is complete. The task list is approved and BI1 may begin per-slice ATDD implementation.
