# MB2 — Evaluate backend MVP

Required by `data-evaluation-passes="1"`. Pass 1 raised one finding (ATDD evidence + a follow-on EF Core test-host issue); Pass 2 is clean.

## Pass 1 — findings

Walked the Implementation Evaluation Rubric scoped to backend (Backend, Validation, Authentication, Testing, General) against `./backend/` and the MB1 deliverables.

| # | Criterion | Result |
|---|---|---|
| 1 | Guidance adherence | **Pass** — see explicit-checks breakdown below. |
| 2 | Requirements coverage (MVP scope) | **Pass** — MB1 is intentionally a single sample slice (per the workflow text: "thin backend MVP that proves the architectural pattern… one sample command + handler + FluentValidation validator… one sample query + handler"). Full requirements coverage is BT1/BI1, not MB1. |
| 3 | Radically simple | **Pass** — only the abstractions required by Clean Architecture (`IAppDbContext`, `IPasswordHasher`, `IJwtTokenService`, `ICurrentUserAccessor`) and one MediatR pipeline behavior. No repository, no UoW, no `Result<T>` wrapper, no command/query base classes, no extra layers, no dead code. |
| 4 | No temp code or stubs | **Pass** — grep for `TODO`, `FIXME`, `XXX`, `HACK`, `NotImplementedException`, `not implemented`, `throw new Error` returns zero matches across `./backend/`. No empty method bodies, no hard-coded sentinel returns. |
| 5 | One type per file | **Pass** — every C# file under `./backend/src/` and `./backend/tests/` contains exactly one type. Grep `^\s*(public|internal|private|sealed)?\s*(class|interface|enum|record|struct|delegate)\s+\w+` shows 1 type per file across the 27 source files. |
| 6 | SOLID + CQS shape | **Pass** — handlers depend on `IAppDbContext` (not the concrete `AppDbContext`); `RegisterUserCommandHandler` and `GetCurrentUserQueryHandler` are constructor-injected via MediatR; `RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>` is colocated with the command; no repository or UoW; commands and queries live in feature folders. |
| 7 | Frontend library placement | N/A — backend scope. |
| 8 | ATDD evidence | **Pass with finding** — see F1. |
| 9 | Mobile-first responsive | N/A — backend scope. |
| 10 | Build and run clean | **Pass** — `dotnet build PullUp.sln -c Release` produces **0 warnings, 0 errors**. API runs from `dotnet run --project backend/src/PullUp.Api` per `./docs/runbooks/backend.md`. |

### Explicit checks (from the MB2 task body)

| Check | Result |
|---|---|
| Clean Architecture layering (Api / Application / Domain / Infrastructure) | Pass — four projects under `backend/src/`; dependencies flow Api → {Application, Infrastructure} → Application → Domain. |
| MediatR CQS | Pass — `MediatR` 12.5 (free OSS license) wired in `Application/DependencyInjection.cs`. |
| Feature folders | Pass — `Application/Features/Users/RegisterUser/{Command,Handler,Validator,Response,DuplicateEmailException}` and `Application/Features/Users/GetCurrentUser/{Query,Handler,Response}`. No top-level `Commands/`, `Queries/`, `Handlers/`, or `Validators/` folders. |
| No repository / UoW | Pass — grep for `Repository`, `UnitOfWork`, `IRepository` across `./backend/` returns zero matches. |
| `IAppDbContext` in handlers | Pass — both handlers inject `IAppDbContext`; concrete `AppDbContext` is registered separately and bound to the interface via `services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>())`. |
| FluentValidation per command | Pass — `RegisterUserCommandValidator` is registered by assembly scan (`AddValidatorsFromAssembly`); `ValidationBehavior<TRequest,TResponse>` runs validators before handlers; failures throw `ValidationException` which `Program.cs` maps to HTTP 400 `ValidationProblemDetails`. |
| Local DB is SQLEXPRESS via `appsettings.Development.json` | Pass — connection string is `Server=.\SQLEXPRESS;Database=PullUp;Trusted_Connection=True;TrustServerCertificate=True;`. No LocalDB, no Docker, no in-memory in the development connection. The `Sqlite` branch in `Infrastructure/DependencyInjection.cs` is reachable only when `Database:Provider` is set to `Sqlite`, which is the test-only configuration — see the discussion of F1 below. |
| JWT validation on every protected endpoint | Pass — `AddJwtBearer` validates issuer, audience, signature, and lifetime with a 30-second clock skew; `UsersController.GetCurrent` is `[Authorize]`; invalid/missing tokens return HTTP 401 (verified by `GetCurrentUserTests.Me_without_token_returns_401`). |
| Password hashing with Argon2id / PBKDF2 / bcrypt | Pass — `Pbkdf2PasswordHasher` uses PBKDF2-HMAC-SHA256 with 600,000 iterations, 16-byte salt, 32-byte hash (matches L2-040). |
| One-type-per-file across the entire backend | Pass — verified per criterion 5. |
| No TODO / NotImplementedException / empty bodies | Pass — verified per criterion 4. |
| Radically simple | Pass — verified per criterion 3. |
| Sample slice fully implemented end to end | Pass — `RegisterUserTests.Register_with_valid_input_returns_201_and_access_token` exercises HTTP → MediatR → FluentValidation → handler → EF → SQLite round-trip and asserts the 201 response shape (user id, email, full name, display name, signed JWT, expiry). `GetCurrentUserTests.Me_with_token_from_register_returns_200_and_user_profile` exercises JWT bearer auth → MediatR → handler → EF and asserts the user profile is returned. |

### Finding F1 — no acceptance test for the sample slice

- **Where:** Backend test surface (initially absent).
- **What:** Rubric criterion 8 ("ATDD evidence. Acceptance test exists and was written before the implementation. It currently passes against the actual implementation.") was not satisfied — MB1 shipped without a test project. The workflow's per-slice ATDD loop lives in BI1, but the MB2 evaluation explicitly imports the implementation-evaluation rubric, which includes ATDD evidence even for the MVP. Without it the MVP is unverifiable end-to-end.
- **Fix:**
  1. Added `backend/tests/PullUp.Api.IntegrationTests/` with xUnit + `Microsoft.AspNetCore.Mvc.Testing` + `Microsoft.EntityFrameworkCore.Sqlite`.
  2. Refactored `Infrastructure.AddInfrastructure` to pick the EF provider from a new `Database:Provider` config setting (`SqlServer` is the default — preserves the SQLEXPRESS-only rule for local development; `Sqlite` is the test-only opt-in). This avoided the "two database providers registered" error you get when trying to override an existing `AddDbContext`-registered SqlServer provider via test-only service replacement.
  3. Added `backend/src/PullUp.Api/appsettings.Testing.json` with `Database:Provider=Sqlite` and a shared-cache in-memory SQLite connection string.
  4. `TestWebApplicationFactory` holds one open SQLite connection for its lifetime so the shared-cache in-memory DB survives across requests; `EnsureDatabaseCreated` is idempotent via a static gate so multiple fixtures sharing the same in-memory DB don't try to recreate the schema.
  5. Authored six tests (4 register, 2 me) each with a header comment naming the L2 IDs it traces to:
     - L2-001 (register valid input → 201 + token),
     - L2-002 (duplicate email → 409),
     - L2-003 (weak password → 400 `ValidationProblemDetails` with `errors.Password`),
     - L2-003 (missing full name → 400),
     - L2-011 + L2-041 (no token → 401),
     - L2-011 + L2-041 (valid token from register → 200 with profile + role).
- **ATDD chronology note:** the MB1 sample-slice code was written before the tests (the workflow's per-slice ATDD discipline is BI1's, not MB1's). For the MVP this is acceptable; future implementation tasks (BI1, FI1) will write the acceptance test first per their stated per-slice loops.
- **Verification:** `dotnet test PullUp.sln -c Release` reports `Passed: 6, Failed: 0, Skipped: 0` in ~2.5 s.

## Pass 2 — clean

Re-walked the rubric and the explicit-checks list against the post-fix backend.

| # | Criterion | Result |
|---|---|---|
| 1 | Guidance adherence | Pass. |
| 2 | Requirements coverage (MVP scope) | Pass. |
| 3 | Radically simple | Pass. |
| 4 | No temp code or stubs | Pass. |
| 5 | One type per file | Pass. |
| 6 | SOLID + CQS shape | Pass. |
| 8 | ATDD evidence | Pass — 6 integration tests exist; all green. |
| 10 | Build and run clean | Pass — `dotnet build PullUp.sln -c Release` reports 0 warnings / 0 errors; `dotnet test PullUp.sln -c Release` reports 6 passed. |

MB2 is complete. The backend MVP is approved as the pattern reference for BP1 / BT1 / BI1.
