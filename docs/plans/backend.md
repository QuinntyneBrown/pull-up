# Backend implementation plan — Pull Up API

This plan turns the 24 L1 / 67 L2 requirements into a concrete, end-to-end .NET 10 implementation grounded in the MB1 sample slice's patterns. Every section below cites the L2 IDs it implements (so traceability is preserved through BT1 and BI1) and the Implementation-Guidance rules it must satisfy. The MB1 reference shape (Clean Architecture, MediatR, FluentValidation, `IAppDbContext`, PBKDF2 hashing, `JsonWebTokenHandler` JWT, feature folders, one-type-per-file) is taken as given — no plan item re-litigates it.

## 1. Module / project layout

The MVP's four-project split is the final shape:

```
backend/
  src/
    PullUp.Domain/            entities, value objects, domain enums
    PullUp.Application/       MediatR commands/queries/handlers/validators (feature folders), abstractions, behaviors
    PullUp.Infrastructure/    EF Core DbContext, security services (hasher, JWT, current user), email no-op, audit log writer
    PullUp.Api/               ASP.NET Core controllers, request DTOs, Program.cs composition root, configuration files
  tests/
    PullUp.Api.IntegrationTests/    xUnit + WebApplicationFactory against SQLite in-memory (shared cache); already in place from MB2
    PullUp.Application.UnitTests/   xUnit for handler-level unit tests where useful (no separate runner — same xUnit setup)
```

Dependency direction is enforced as today: Api → Infrastructure → Application → Domain; Application → Domain only. Tests reference Api (which transitively pulls everything).

## 2. Domain layer

Adds these entities/enums to `PullUp.Domain`:

| Type | Notes | L2 refs |
|---|---|---|
| `Users/User` (already exists) | Add fields for: `EmailVerifiedAt: DateTimeOffset?`, `LastPasswordChangedAt: DateTimeOffset`, `LockoutUntil: DateTimeOffset?`, `FailedSignInAttempts: int`. Methods: `Register`, `ChangePassword(hash)`, `RecordFailedSignIn(now)`, `RecordSuccessfulSignIn`, `RequestEmailChange(newEmail)`, `ConfirmEmailChange()`, `Tombstone()` (for L2-015). | L2-001..L2-017, L2-040, L2-042 |
| `Users/Role` (already exists) | No change. | L2-045, L2-046 |
| `Users/PendingEmailChange` | Sub-record on User: `NewEmail`, `Token`, `ExpiresAt`. | L2-013 |
| `Users/PasswordResetToken` | Standalone aggregate: `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `UsedAt?`. Stores hash of token, not token itself. | L2-008, L2-009, L2-010 |
| `Users/RefreshToken` | `Id`, `UserId`, `TokenHash`, `IssuedAt`, `ExpiresAt`, `RevokedAt?`, `ReplacedByTokenId?`. | L2-006, L2-007, L2-009 |
| `Events/Event` | `Id`, `HostId`, `Title`, `StartsAtUtc`, `EndsAtUtc?`, `Location`, `Description`, `AllowPlusOne`, `ShowGuestList`, `Status` (Scheduled/Cancelled), `CreatedAt`, `UpdatedAt`. Methods: `Create`, `UpdateDetails`, `Cancel`. | L2-007 through L2-030 |
| `Events/EventStatus` enum | `Scheduled = 1`, `Cancelled = 2`. | L2-029 |
| `Events/Invitation` | `Id`, `EventId`, `UserId?` (nullable for email-only invites), `InvitedEmail`, `InvitedAt`, `RemovedAt?`. | L2-031, L2-032, L2-033 |
| `Events/Rsvp` | `Id`, `EventId`, `UserId`, `Status` (Going/Maybe/CantGo), `Note?`, `UpdatedAt`. | L2-012, L2-034, L2-035, L2-036 |
| `Events/RsvpStatus` enum | `Going = 1`, `Maybe = 2`, `CantGo = 3`. | L2-034 |
| `Audit/AuditLogEntry` | `Id`, `Event` (string enum), `ActorUserId?`, `OccurredAt`, `CorrelationId`, `Outcome`, `MetadataJson`. | L2-005, L2-010, L2-043, L2-060, L2-061 |
| `Notifications/NotificationPreference` | Per-user; `UserId`, `NewInvitations`, `EventReminders`, `RsvpChanges`. Created with all=true at registration. | L2-016, L2-017 |

All entity types stay in their own files; navigation properties between aggregates are intentionally avoided — handlers compose data via `IAppDbContext` queries (no `DbSet.Include` chains across aggregates).

## 3. `IAppDbContext` and persistence

**Local + production database**: Microsoft SQL Server, with **SQL Server Express (SQLEXPRESS)** as the canonical local-development instance per the Implementation Guidance. The local connection string `Server=.\SQLEXPRESS;Database=PullUp;Trusted_Connection=True;TrustServerCertificate=True;` is committed in `backend/src/PullUp.Api/appsettings.Development.json` (already in place from MB1). No LocalDB, no Docker SQL Server, no in-memory provider for local development; SQLite is used **only** by `PullUp.Api.IntegrationTests` via the `Database:Provider=Sqlite` opt-in introduced in MB2. Higher environments override the connection string via configuration.

Extend `IAppDbContext` to expose the new aggregate roots:

```csharp
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Event> Events { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<Rsvp> Rsvps { get; }
    DbSet<AuditLogEntry> AuditLog { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`AppDbContext` adds one `IEntityTypeConfiguration<T>` per type in `Infrastructure/Persistence/Configurations/` (one file per type). All indexes and constraints are declared there, not via attributes — keeps the Domain pure POCOs.

Indexes:
- `Users(Email)` unique (already in MB1).
- `PasswordResetTokens(TokenHash)` unique.
- `RefreshTokens(TokenHash)` unique; `RefreshTokens(UserId, RevokedAt)` for lookup.
- `Events(HostId)`; `Events(StartsAtUtc)` for date-range queries.
- `Invitations(EventId)`; `Invitations(UserId)`; partial unique on `(EventId, UserId)` where `UserId IS NOT NULL`.
- `Rsvps(EventId, UserId)` unique.
- `AuditLog(OccurredAt)` + `AuditLog(ActorUserId, OccurredAt)`.

## 4. Application layer — feature folder inventory

All under `PullUp.Application/Features/<Feature>/<Slice>/` per the implementation guidance. Each slice has Command/Query + Handler + (FluentValidation) Validator for commands + Response.

### Features/Users/

| Slice | Type | Files | L2 refs | Notes |
|---|---|---|---|---|
| `RegisterUser` | Command | exists from MB1 | L2-001, L2-002, L2-003, L2-017 | Extend to also create default `NotificationPreference` row in the same transaction; emit `SIGN_UP_SUCCESS` audit entry. |
| `SignInUser` | Command | new | L2-004, L2-005, L2-042, L2-043 | Verifies password via `IPasswordHasher.Verify`, applies rate-limit policy via `ISignInRateLimiter`, returns access + refresh tokens, writes audit (success or failure). |
| `RefreshAccessToken` | Command | new | L2-006 | Rotates refresh token; revokes old; issues new pair. |
| `SignOut` | Command | new | L2-007 | Revokes the supplied refresh token; writes audit `SIGN_OUT`. |
| `RequestPasswordReset` | Command | new | L2-008, L2-010 | Generates 256-bit token, stores HMAC-SHA-256 hash, emails the raw value via `IEmailSender` no-op, always returns 202. |
| `CompletePasswordReset` | Command | new | L2-003, L2-009, L2-010 | Validates token unused + unexpired; updates password hash; revokes all RefreshTokens for the user; writes audit. |
| `GetCurrentUser` | Query | exists from MB1 | L2-011 | No change. |
| `UpdateProfile` | Command | new | L2-012 | Updates fullName + displayName with length validators. |
| `RequestEmailChange` | Command | new | L2-013 | Requires current password; stores pending email + verification token. |
| `ConfirmEmailChange` | Command | new | L2-013 | Validates token; promotes pending to primary. |
| `DeleteAccount` | Command | new | L2-014, L2-015 | Re-verifies password; tombstones user; cancels hosted future events (cascade via `CancelEvent` reused per-event); writes audit `ACCOUNT_DELETED`. |
| `UpdateNotificationPreferences` | Command | new | L2-016 | Toggles the three flags; idempotent. |

### Features/Events/

| Slice | Type | L2 refs | Notes |
|---|---|---|---|
| `CreateEvent` | Command | L2-018, L2-019, L2-020, L2-021, L2-031, L2-032, L2-037 | Persists Event + initial Invitations + host Rsvp(Going); fires `NewInvitation` notification per invitee. Validator enforces field lengths and future-date rule. |
| `GetEvent` | Query | L2-023, L2-024, L2-027, L2-036 | Returns event projection with host details, guest list (respecting `ShowGuestList`), aggregate RSVP counts, current-user RSVP, host actions flag. Authorization: host or invitee. |
| `ListMyEvents` | Query | L2-022, L2-024, L2-025 | Returns paged, grouped projection (`thisWeek`, `laterThisMonth`, `nextMonth`, `past`) filtered by scope (`All`/`Hosting`/`Invited`/`Past`). Uses `IAppDbContext.Events` joined to `Invitations` + `Rsvps`. |
| `UpdateEvent` | Command | L2-026, L2-027, L2-028 | Host-only; updates editable fields; on date/location/time change emits `EventUpdated` notifications to invitees with `NewInvitations` pref on. |
| `CancelEvent` | Command | L2-029, L2-030 | Host-only; sets `Status = Cancelled`; emits `EventCancelled` notification to invitees. |
| `AddInvitee` | Command | L2-031, L2-032, L2-037 | Host-only; creates `Invitation`; if email-only, the invitation links to the future user on register. |
| `RemoveInvitee` | Command | L2-033 | Host-only; sets `Invitation.RemovedAt`; clears any existing RSVP. |
| `SetRsvp` | Command | L2-034, L2-035, L2-039 | Invitee-only; upserts `Rsvp`; rejects with HTTP 409 if event is past; emits notification to host if `RsvpChanges` pref is on. |

### Features/Notifications/

| Slice | Type | L2 refs | Notes |
|---|---|---|---|
| `DispatchInvitationNotification` | Command | L2-037 | Internal; invoked from `CreateEvent`, `AddInvitee`, `UpdateEvent`. Looks up per-user pref; logs to `INotificationSender` no-op. |
| `DispatchEventReminders` | Command | L2-038 | Triggered by `IHostedService` scheduler that runs once per minute; finds events starting in [24h, 24h+1min] window with attending users; dispatches one reminder per Rsvp. Idempotency key prevents duplicate sends across restarts. |
| `DispatchRsvpChangeNotification` | Command | L2-039 | Internal; invoked from `SetRsvp`. |

### Cross-cutting (`Application/Behaviors/` and `Application/Common/`)

- `ValidationBehavior<TRequest,TResponse>` — exists from MB1.
- `AuthorizationBehavior<TRequest,TResponse>` — new. Reads an optional `IAuthorizationRequirement` marker on the request; if present, resolves `IAuthorizationService` to authorize current user against the requirement. Used by handlers that need entity-level auth (e.g., host-only on UpdateEvent).
- `AuditingBehavior<TRequest,TResponse>` — new. Annotates the request with an `[AuditedAction("EVENT_DELETED")]` attribute; on success writes an audit entry. Failure path also writes audit (with `Outcome=FAILURE`) for the security-sensitive actions enumerated in L2-060.
- `Common/Exceptions/`: `DuplicateEmailException` (exists), `NotFoundException`, `NotAuthorizedException`, `EventPassedException`. One type per file.

### `Application/Abstractions/` additions

- `IPasswordHasher` (exists) — no change.
- `IJwtTokenService` — extend with `IssueRefreshToken(User)` returning `(string raw, RefreshToken record)` so the handler can persist the hash and return the raw value.
- `ICurrentUserAccessor` (exists) — no change.
- `IEmailSender` — new. Methods for password-reset email, email-change verification email, event invitation email, event reminder email, event cancellation email. Implementation in Infrastructure is a logging no-op (the optional deferred integration documented below).
- `INotificationSender` — new. For in-app notifications. MVP implementation is a logging no-op.
- `ISignInRateLimiter` — new. `RegisterFailedAttempt(email)` and `EnsureNotLocked(email)`; backed by `IMemoryCache` for now.
- `IAuditLogger` — new. `Write(AuditLogEntryDescriptor)`. Implementation persists to `AppDbContext.AuditLog`.
- `ITokenHasher` — new. HMAC-SHA-256 hash of opaque tokens for the password-reset + refresh-token stores; uses a server-side pepper from configuration so token theft requires both DB access and configuration access.

## 5. Infrastructure layer

Adds these files (all in their own file, in their own folder by concern):

- `Persistence/Configurations/<Entity>Configuration.cs` — one per entity.
- `Persistence/Migrations/<timestamp>_<Name>.cs` — sequenced as in §8.
- `Security/Pbkdf2PasswordHasher.cs` — exists.
- `Security/JwtTokenService.cs` — extend per §4.
- `Security/RefreshTokenGenerator.cs` — 256-bit cryptographically-random opaque tokens.
- `Security/Pbkdf2TokenHasher.cs` — implements `ITokenHasher` with HMAC-SHA-256 + pepper.
- `Security/SignInRateLimiter.cs` — implements `ISignInRateLimiter` over `IMemoryCache`.
- `Security/HttpContextCurrentUserAccessor.cs` — exists.
- `Notifications/LoggingEmailSender.cs` — implements `IEmailSender`, logs each intended send at Information.
- `Notifications/LoggingNotificationSender.cs` — implements `INotificationSender`.
- `Auditing/AuditLogger.cs` — implements `IAuditLogger` against `AppDbContext.AuditLog`.
- `Reminders/EventReminderHostedService.cs` — `BackgroundService` that loops once per minute and dispatches reminders via MediatR.

## 6. API layer — controller surface

All controllers under `PullUp.Api/Controllers/`, one file per controller; one HTTP-request DTO per command kept in `PullUp.Api/Requests/` (kept simple — DTOs are transport shapes only, no validation attributes per the guidance).

| Controller | Endpoint | Auth | Command/Query | L2 refs |
|---|---|---|---|---|
| `UsersController` | `POST /api/users` | anonymous | `RegisterUserCommand` (exists) | L2-001..L2-003 |
| `UsersController` | `GET /api/users/me` | bearer | `GetCurrentUserQuery` (exists) | L2-011 |
| `UsersController` | `PUT /api/users/me/profile` | bearer | `UpdateProfileCommand` | L2-012 |
| `UsersController` | `POST /api/users/me/email-change` | bearer | `RequestEmailChangeCommand` | L2-013 |
| `UsersController` | `POST /api/users/me/email-change/confirm` | bearer | `ConfirmEmailChangeCommand` | L2-013 |
| `UsersController` | `DELETE /api/users/me` | bearer | `DeleteAccountCommand` | L2-014, L2-015 |
| `UsersController` | `PUT /api/users/me/notification-preferences` | bearer | `UpdateNotificationPreferencesCommand` | L2-016 |
| `AuthController` | `POST /api/auth/sign-in` | anonymous | `SignInUserCommand` | L2-004, L2-005, L2-042 |
| `AuthController` | `POST /api/auth/refresh` | anonymous | `RefreshAccessTokenCommand` | L2-006 |
| `AuthController` | `POST /api/auth/sign-out` | anonymous | `SignOutCommand` | L2-007 |
| `AuthController` | `POST /api/auth/password-reset` | anonymous | `RequestPasswordResetCommand` | L2-008 |
| `AuthController` | `POST /api/auth/password-reset/confirm` | anonymous | `CompletePasswordResetCommand` | L2-009 |
| `EventsController` | `GET /api/events` | bearer | `ListMyEventsQuery` | L2-022, L2-024, L2-025 |
| `EventsController` | `POST /api/events` | bearer | `CreateEventCommand` | L2-018..L2-021 |
| `EventsController` | `GET /api/events/{id}` | bearer | `GetEventQuery` | L2-023, L2-027 |
| `EventsController` | `PUT /api/events/{id}` | bearer | `UpdateEventCommand` | L2-026..L2-028 |
| `EventsController` | `POST /api/events/{id}/cancel` | bearer | `CancelEventCommand` | L2-029, L2-030 |
| `EventsController` | `POST /api/events/{id}/invitees` | bearer | `AddInviteeCommand` | L2-031, L2-032 |
| `EventsController` | `DELETE /api/events/{id}/invitees/{inviteeId}` | bearer | `RemoveInviteeCommand` | L2-033 |
| `EventsController` | `PUT /api/events/{id}/rsvp` | bearer | `SetRsvpCommand` | L2-034..L2-036 |
| `HealthController` | `GET /health/live` | anonymous | inline (no MediatR) | L2-064 |
| `HealthController` | `GET /health/ready` | anonymous | inline (checks DB) | L2-064 |

All protected endpoints are `[Authorize]` at controller level; `[AllowAnonymous]` overrides where noted. The `AuthController` endpoints intentionally don't require auth — the rate limiter (L2-042) handles abuse on `/sign-in`.

## 7. Authentication flow detail

- **Password storage**: `Pbkdf2PasswordHasher` (PBKDF2-HMAC-SHA256, 600,000 iterations, 16-byte salt, 32-byte hash) — exists.
- **Sign-up**: `RegisterUser` → hashes password → persists `User` + default `NotificationPreference` → issues access + refresh tokens (refresh persisted as hash) → writes `SIGN_UP_SUCCESS` audit.
- **Sign-in**: `SignIn` → `EnsureNotLocked(email)` → look up by email → verify password → on success: clear `FailedSignInAttempts`, issue tokens, write audit; on failure: increment counter, write `SIGN_IN_FAILURE`, return HTTP 401.
- **Rate limiting**: `SignInRateLimiter` rejects if ≥5 failures within 60 s for the same email — returns 429 with `Retry-After: 60` (L2-042).
- **Token refresh**: `RefreshAccessToken` validates the supplied refresh token hash exists, is unrevoked, and is unexpired; rotates (revoke old, issue new) and returns new pair.
- **Sign-out**: revokes the supplied refresh token.
- **Password reset**: `RequestPasswordReset` always returns 202; if email matches, generates 256-bit token, stores hash, sends email. `CompletePasswordReset` validates and rotates password; revokes all refresh tokens for the user.
- **JWT**: `JsonWebTokenHandler` issues HS256 access tokens valid 60 min; claims = `sub`, `email`, `jti`, `role`. JWT validation in `AddJwtBearer` checks issuer / audience / signature / lifetime with 30 s clock skew (L2-041).
- **RBAC**: `Roles` claim drives `[Authorize(Roles = "Admin")]` where needed; user-vs-host authorization at the resource level lives in handler logic (or `IAuthorizationRequirement` if it's clean). The MVP shipped two roles (User, Admin) — keep that.

## 8. Migration sequencing

One `dotnet ef migrations add <Name>` invocation per logical schema change so rollbacks stay tractable:

1. `InitialCreate` — exists (Users only).
2. `AddPasswordResetTokens`.
3. `AddRefreshTokens` + add `LastPasswordChangedAt`, `LockoutUntil`, `FailedSignInAttempts`, `EmailVerifiedAt` to `Users`.
4. `AddNotificationPreferences`.
5. `AddEventsAndInvitations`.
6. `AddRsvps`.
7. `AddAuditLog`.
8. `AddPendingEmailChanges`.

Each migration is committed in its own commit so `dotnet ef migrations remove` works cleanly. The local-dev runbook command (`dotnet ef database update`) applies them all.

## 9. Deferred integrations (logging no-op services)

These are the only items in scope that are explicitly deferred per the implementation guidance's "no stubs… optional integrations explicitly deferred". Each is a single-purpose class named after its intent so the production wiring point is obvious:

- `LoggingEmailSender : IEmailSender` — logs each intended message at `Information`. Used for password-reset emails, email-change verification, invitation, reminder, cancellation. Production swap-in is a real SMTP/SES/SendGrid client; that work is **out of scope** until the deployment-driven follow-up. Documented in BI1 evaluation notes for every slice that exercises it.
- `LoggingNotificationSender : INotificationSender` — same shape for in-app notifications. The system's in-app notification surface (e.g., a `GET /api/notifications` endpoint, a websocket push) is **explicitly out of scope** for the MVP; only the dispatcher and the per-user preference gating are.

Both no-ops are wired in `Infrastructure/DependencyInjection.cs`; tests inject substitutes via `WebApplicationFactory.ConfigureTestServices` when they need to assert dispatch behavior.

## 10. Cross-cutting non-functionals

| L2 area | Plan item |
|---|---|
| HTTPS enforced (L2-049) | `app.UseHttpsRedirection()` + HSTS via `app.UseHsts()` in non-Development environments; the dev `appsettings.Development.json` keeps the HTTP listener as well so curl examples in the backend runbook work. |
| Sensitive data not in logs (L2-044, L2-050) | A `RequestLoggingFilter` redacts `password`, `currentPassword`, `newPassword`, `token`, `refreshToken`, `resetToken` keys in any incoming JSON body before the request log line is emitted. The default ASP.NET Core request logger is otherwise configured to **not** log bodies. |
| Performance (L2-058, L2-059) | `ListMyEventsQuery` projects directly to a flat DTO via `Select` to avoid loading full graphs. Read endpoints use `AsNoTracking()`. The 99th-percentile budgets are runtime-measurable goals, not architectural constraints — verified in TP3. |
| Audit retention (L2-061) | No automated archival in the MVP; the `AuditLog` table retains everything. A `RetentionWorker` BackgroundService is **out of scope** for BI1 but the migration leaves `OccurredAt` indexed so a later worker can do bulk archival. |
| Output encoding (L2-066) | Server returns user-supplied content as plain JSON string values; the frontend's Angular interpolation handles escaping. The backend never pre-renders HTML for clients. |
| Dependency hygiene (L2-067) | CI step `dotnet list package --vulnerable --include-transitive` fails on `High`/`Critical`. Captured as a CI work item in DP1. |

## 11. What this plan does **not** introduce

To stay radically simple, the following common-but-unnecessary patterns are explicitly out of scope:

- Repository / Unit-of-Work classes (forbidden by guidance).
- A generic `Result<T>` / `OneOf` wrapper. Handlers throw domain exceptions, the API maps them to HTTP via the existing exception middleware.
- A MediatR `INotification` event-bus. All cross-feature dispatch is direct MediatR command sends.
- A generic CRUD base controller.
- A separate `Application.UnitTests` test project with mocks. xUnit unit tests, where useful, live alongside integration tests in `PullUp.Api.IntegrationTests` until size justifies a split.
- A DDD aggregate-event publishing mechanism. Domain methods mutate state; the handler is responsible for any downstream dispatch.
- A `Mediator.IPipelineBehavior` for caching, logging, or retry. Cross-cutting today = validation + authorization + auditing only.

## 12. Mapping table — every L2 → plan item

(Section IDs reference §2..§10 above. Empty cell means the L2 is satisfied by an MB1-baseline rule rather than a new plan item.)

| L2 | Where it's implemented |
|---|---|
| L2-001..L2-003 | §4 Features/Users/RegisterUser; §2 User entity defaults; §10 password redaction. |
| L2-004 | §4 SignInUser; §6 AuthController. |
| L2-005, L2-043 | §4 SignInUser + §4 AuditingBehavior + §5 AuditLogger. |
| L2-006 | §4 RefreshAccessToken; §5 RefreshTokenGenerator + Pbkdf2TokenHasher. |
| L2-007 | §4 SignOut. |
| L2-008..L2-010 | §4 RequestPasswordReset + CompletePasswordReset; §2 PasswordResetToken. |
| L2-011, L2-012, L2-013 | §4 Features/Users (GetCurrentUser exists / UpdateProfile / Request+ConfirmEmailChange). |
| L2-014, L2-015 | §4 DeleteAccount; tombstone method on User. |
| L2-016, L2-017 | §4 UpdateNotificationPreferences; §2 NotificationPreference. |
| L2-018..L2-021 | §4 CreateEvent + validator. |
| L2-022, L2-024, L2-025 | §4 ListMyEventsQuery; flat DTO. |
| L2-023, L2-027 | §4 GetEventQuery; host-vs-invitee authorization. |
| L2-026..L2-028 | §4 UpdateEvent + DispatchInvitationNotification trigger. |
| L2-029, L2-030 | §4 CancelEvent + DispatchInvitationNotification(EventCancelled). |
| L2-031..L2-033 | §4 AddInvitee + RemoveInvitee. |
| L2-034..L2-036 | §4 SetRsvp. |
| L2-037 | §4 DispatchInvitationNotification. |
| L2-038 | §4 DispatchEventReminders + §5 EventReminderHostedService. |
| L2-039 | §4 DispatchRsvpChangeNotification. |
| L2-040 | Pbkdf2PasswordHasher (exists). |
| L2-041 | AddJwtBearer config (exists). |
| L2-042 | §4 SignInUser + §5 SignInRateLimiter. |
| L2-044, L2-050 | §10 redaction filter. |
| L2-045, L2-046 | Handler-level authorization + `[Authorize(Roles=...)]`. |
| L2-047, L2-048 | ValidationBehavior + ValidationProblemDetails (exists). |
| L2-049 | UseHttpsRedirection + UseHsts. |
| L2-051..L2-054, L2-055..L2-057 | Frontend concerns — not in backend plan. |
| L2-058, L2-059 | §10 — query shape + AsNoTracking. |
| L2-060, L2-061 | §2 AuditLogEntry + §5 AuditLogger + §10. |
| L2-062, L2-063 | Frontend concerns. |
| L2-064 | §6 HealthController. |
| L2-065 | Backend runbook (exists). |
| L2-066 | §10 — backend returns plain JSON, no HTML pre-render. |
| L2-067 | §10 — CI dotnet list package --vulnerable; carried into DP1. |

Every L2 in the requirements is either fully addressed here or explicitly tagged as a frontend concern. BT1 will break this plan into vertically-sliced tasks; BI1 will implement each slice ATDD-first against the integration test project added in MB2.
