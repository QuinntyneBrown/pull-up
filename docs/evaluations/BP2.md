# BP2 — Evaluate backend plan

Required by `data-evaluation-passes="1"`. Pass 1 raised one finding (a missing explicit SQLEXPRESS statement in the plan body); Pass 2 confirms clean.

## Pass 1 — findings

Walked the BP2 explicit-checks list and the full L2 coverage against `./docs/plans/backend.md` and the Implementation Guidance.

| # | Check | Result |
|---|---|---|
| 1 | Clean Architecture layering planned | **Pass** — §1 names the four projects (Api / Application / Domain / Infrastructure) and the dependency direction (Api → Infrastructure → Application → Domain; Application → Domain only). |
| 2 | CQS via MediatR planned | **Pass** — §4 organizes every mutating action as a `Command` and every read as a `Query`, each dispatched via MediatR. |
| 3 | Per-feature folder layout planned | **Pass** — §4 explicitly groups commands/queries/handlers/validators under `PullUp.Application/Features/<Feature>/<Slice>/` (Users, Events, Notifications). The plan opens §4 with: "All under `PullUp.Application/Features/<Feature>/<Slice>/` per the implementation guidance." No top-level `Commands/`, `Queries/`, `Handlers/`, or `Validators/` folders appear anywhere in the plan. |
| 4 | No repository or unit-of-work | **Pass** — §11 names "Repository / Unit-of-Work classes (forbidden by guidance)" as explicitly out of scope. |
| 5 | FluentValidation per command planned | **Pass** — §4 specifies "Each slice has Command/Query + Handler + (FluentValidation) Validator for commands + Response", and §10 + §12 cite the existing `ValidationBehavior` for L2-047/048. |
| 6 | `IAppDbContext` abstraction planned | **Pass** — §3 declares the extended `IAppDbContext` interface with every new `DbSet`. Handlers depend on the interface, not on `AppDbContext`. |
| 7 | Auth flow planned (local username/password) | **Pass** — §7 walks the full local-credential flow: PBKDF2 hashing (existing), sign-up, sign-in, rate limiting, token refresh, sign-out, password reset. §12 maps L2-014 / L2-040–046 to the relevant items. RBAC is named (User + Admin roles, claims-driven). |
| 8 | SQL Server (SQLEXPRESS for local) with connection string in `appsettings.Development.json` | **Pass with finding** — see F1 below. |
| 9 | One-type-per-file convention assumed | **Pass** — §1 states: "MB1 reference shape (Clean Architecture, MediatR, FluentValidation, `IAppDbContext`, PBKDF2 hashing, `JsonWebTokenHandler` JWT, feature folders, one-type-per-file) is taken as given — no plan item re-litigates it." The convention is preserved by referring to per-file artifacts throughout (e.g., §2 "one file per type", §4 "Common/Exceptions/: … one type per file", §5 "all in their own file"). |
| 10 | Deferred integrations explicitly enumerated as no-op logging services | **Pass** — §9 names exactly two: `LoggingEmailSender : IEmailSender` and `LoggingNotificationSender : INotificationSender`, each with a stated production swap-in. No other stubs are introduced. |
| 11 | No speculative abstractions in the plan | **Pass** — §11 explicitly excludes `Result<T>` / `OneOf` wrappers, MediatR `INotification` event bus, generic CRUD base controller, DDD aggregate-event publishing, separate `Application.UnitTests` project, and caching/logging/retry pipeline behaviors. Only validation + authorization + auditing behaviors are introduced. |

### L2 requirements coverage

§12's mapping table cites every L2 in `./docs/specs/L2.md`. Spot-checked:

- L2-001..L2-010 → Features/Users (Register/SignIn/Refresh/SignOut/PasswordReset).
- L2-011..L2-017 → Features/Users (GetCurrentUser/UpdateProfile/EmailChange/DeleteAccount/NotificationPreferences).
- L2-018..L2-039 → Features/Events + Features/Notifications.
- L2-040..L2-046 → §7 auth flow + Infrastructure security services.
- L2-049 → §10 HTTPS row.
- L2-051..L2-057, L2-062..L2-063 → flagged as frontend concerns ✓ (consistent with the workflow split).
- L2-058..L2-059 → §10 performance row.
- L2-060..L2-061 → §2 `AuditLogEntry` + §5 `AuditLogger` + §10 retention row.
- L2-064 → §6 `HealthController` row.
- L2-066 → §10 output-encoding row (backend role: never pre-render HTML).
- L2-067 → §10 dep-hygiene row + DP1 handoff.

No L2 is unaccounted for. No plan-item conflicts with the Implementation Guidance.

### Finding F1 — SQLEXPRESS not explicitly stated in the plan body

- **Where:** §3 (`IAppDbContext` and persistence) and §10 (cross-cutting non-functionals) in the original plan.
- **What:** BP2 check 8 requires the plan to state "SQL Server (SQLEXPRESS for local development) with the local connection string committed in `appsettings.Development.json`". The original plan implied this by treating "MB1 reference shape" as given, but never named SQLEXPRESS or the connection-string location in the plan body itself. A future engineer reading only the plan would not see the local-DB rule until they cross-referenced the MB1 runbook.
- **Severity:** Documentation completeness. Blocking the BP2 explicit check.
- **Fix:** prepended an explicit paragraph to §3 stating: SQLEXPRESS is the canonical local-dev instance; the connection string `Server=.\SQLEXPRESS;Database=PullUp;Trusted_Connection=True;TrustServerCertificate=True;` is committed in `backend/src/PullUp.Api/appsettings.Development.json`; LocalDB / Docker / in-memory are forbidden for local; SQLite is reserved for `PullUp.Api.IntegrationTests` via the `Database:Provider=Sqlite` opt-in from MB2; higher environments override via configuration.

## Pass 2 — clean

Re-walked the BP2 explicit-checks list against the updated plan.

| # | Check | Result |
|---|---|---|
| 1 | Clean Architecture layering | Pass. |
| 2 | CQS via MediatR | Pass. |
| 3 | Per-feature folder layout | Pass. |
| 4 | No repository / UoW | Pass. |
| 5 | FluentValidation per command | Pass. |
| 6 | `IAppDbContext` abstraction | Pass. |
| 7 | Auth flow planned | Pass. |
| 8 | SQLEXPRESS local with appsettings.Development.json | **Pass** — explicit paragraph now in §3. |
| 9 | One-type-per-file | Pass. |
| 10 | Deferred integrations as logging no-ops | Pass. |
| 11 | No speculative abstractions | Pass. |

BP2 is complete. The backend plan is approved; BT1 may break it into vertically-sliced backend tasks.
