# Backend implementation tasks — Pull Up API

Vertically sliced tasks for BI1. Each task is HTTP → MediatR → FluentValidation → handler → `IAppDbContext` → DB (and where needed, audit + notification dispatch). Sizing target: implement + ATDD eval pass in 1–3 loop iterations. Tasks reference `./docs/plans/backend.md` sections and `./docs/specs/L2.md` IDs.

## Conventions for every task in this list

Unless a task explicitly says otherwise, **every task in this list**:

- Goes in the per-feature folder `PullUp.Application/Features/<Feature>/<Slice>/` — one `Command`/`Query` + `Handler` + (for commands) `Validator` + `Response`, **one type per file**.
- Adds HTTP request DTOs to `PullUp.Api/Requests/` (pure transport, no validation attributes); controller actions map DTO → MediatR command/query.
- Adds the controller method to the existing per-feature controller (`UsersController`, `AuthController`, `EventsController`).
- Adds an acceptance test in `backend/tests/PullUp.Api.IntegrationTests/<Feature>/<Slice>Tests.cs` with a header comment `// Traces to: L2-...` listing every L2 it covers. ATDD: the test is committed and seen to fail before the production implementation begins; final state is green.
- Depends on `IAppDbContext` only — no repository, no UoW, no concrete `AppDbContext` from handler code.
- Uses **FluentValidation** for every command's input rules; HTTP 400 surfaces as `ValidationProblemDetails`.
- Adds zero stubs / `TODO` / `NotImplementedException`. Optional integrations route through the existing `LoggingEmailSender` / `LoggingNotificationSender` no-ops (already in the BP1 plan); each evaluation note records which no-op the slice exercised.
- Conforms to Implementation Guidance sections: Backend (.NET), Validation, Authentication, General, Testing. The Authentication-specific rules (PBKDF2, JWT validation, rate limiting, audit) are repeated only on the auth-flow slices below.

The MB2 integration-test harness (xUnit + WebApplicationFactory + SQLite shared-cache) is the test home for every slice — no new test scaffolding is needed.

---

## A note on task shape — "vertical slice" vs "supporting infrastructure"

Most tasks below are end-to-end HTTP-driven vertical slices (HTTP endpoint → command/handler → `IAppDbContext` → DB + audit + notify as needed). A handful — **BT-001, BT-002, BT-005, BT-019** — are flagged here as **supporting infrastructure**: they advance specific L2 requirements (authorization gating, audit log, refresh-token persistence, notification dispatcher) and each ships its own acceptance test, but they do not introduce a new HTTP endpoint on their own. Folding them into their first consuming slice was considered and rejected — the consuming slices would balloon past the "1–3 loop iterations" sizing rule (e.g., BT-006 would have to bring rate limiter + refresh-token store + auditing all at once).

Treating them as separate prerequisite tasks is the explicit choice; each carries enough behavior and test coverage that it is **not** "scaffolding only with no end-to-end value" — it advances real L2 requirements and the acceptance test proves the behavior end-to-end against the running app.

## A. Cross-cutting (do these first — they enable later slices)

### BT-001: Authorization behavior + `[AuthorizationRequirement]` marker
- **Implements:** L2-027, L2-045, L2-046 (host/invitee + RBAC enforcement).
- **Slice:** new `Application/Behaviors/AuthorizationBehavior.cs` runs after `ValidationBehavior`; a marker interface `IAuthorizationRequirement` on a request triggers resolution of a per-requirement `IAuthorizationHandler` (also new, in Application). Throws `NotAuthorizedException` on failure; `Program.cs` maps to HTTP 403.
- **Acceptance test:** `AuthorizationBehaviorTests` — a fake request implementing `IAuthorizationRequirement` is rejected; one without the marker is allowed; an authorized request flows through.
- **Guidance:** SOLID, no temp code, one type per file, FluentValidation untouched.

### BT-002: Auditing behavior + `AuditedAction` attribute + `IAuditLogger`
- **Implements:** L2-005, L2-010, L2-043, L2-060, L2-061.
- **Slice:** new `[AuditedAction("SIGN_IN_FAILURE")]`-style attribute on request types; `AuditingBehavior` writes a row via `IAuditLogger` (new abstraction in Application, implementation `AuditLogger` in Infrastructure writing to `AppDbContext.AuditLog`). On exceptions thrown from the handler, behavior records `outcome=FAILURE` and rethrows. Adds entity `AuditLogEntry` + `IEntityTypeConfiguration<AuditLogEntry>` + EF migration `AddAuditLog`.
- **Acceptance test:** `AuditLoggerTests` — registering a user writes a `SIGN_UP_SUCCESS` row with the actor id; a failed login writes `SIGN_IN_FAILURE` with the email and a `FAILURE` outcome.
- **Guidance:** General, Validation, Backend (.NET), one type per file.

### BT-003: Request body redaction filter
- **Implements:** L2-044, L2-050.
- **Slice:** new `Api/Filters/RequestBodyRedactionFilter` (or `ILogger` enricher) that replaces `password`, `currentPassword`, `newPassword`, `token`, `refreshToken`, `resetToken` fields in the JSON body before logging. Confirms no token/hash/connection-string secret reaches application logs.
- **Acceptance test:** `RedactionTests` — POST a payload with a `password` field, capture the request log line, assert the value is `***REDACTED***`.
- **Guidance:** General, no temp code.

### BT-004: HTTPS / HSTS pipeline + Health endpoints
- **Implements:** L2-049, L2-064.
- **Slice:** add `app.UseHsts()` (non-dev only) to `Program.cs`; flesh out `HealthController` with `GET /health/live` (already inline) and `GET /health/ready` that pings `AppDbContext.Database.CanConnectAsync()` and returns 503 if false.
- **Acceptance test:** `HealthTests` — `/health/live` always 200; `/health/ready` is 200 against the SQLite test DB; if the DbContext is disposed, 503.
- **Guidance:** General.

---

## B. Authentication & session (Auth feature)

### BT-005: Refresh-token storage + token hasher
- **Implements:** Foundation for L2-006, L2-007, L2-009, L2-040, L2-044.
- **Slice:** new entity `Users/RefreshToken` (+ config + EF migration `AddRefreshTokens`); new `ITokenHasher` (Application) and `Pbkdf2TokenHasher` (Infrastructure) using HMAC-SHA-256 with a server-side pepper read from `Jwt:TokenHasherPepper`; new `Infrastructure/Security/RefreshTokenGenerator` (256-bit random); extension to `IJwtTokenService.IssueRefreshToken(User)` returning `(string raw, RefreshToken record)`.
- **Acceptance test:** `RefreshTokenStoreTests` — generates a refresh token, persists the hash, verifies the raw value hashes to the same row, and confirms a revoked token cannot be looked up.
- **Guidance:** Backend (.NET), Authentication, one type per file.

### BT-006: `POST /api/auth/sign-in` (email + password)
- **Implements:** L2-004, L2-005, L2-043.
- **Slice:** new `AuthController.SignIn`, `SignInUserCommand`, `Handler`, `Validator` (email syntax + password not empty), `Response` (access + refresh + user profile). Handler verifies password via `IPasswordHasher.Verify`, generic 401 on failure, audits both success and failure via the `AuditingBehavior`.
- **Acceptance test:** `SignInUserTests` — happy path returns 200 + tokens that work against `/api/users/me`; wrong password returns 401 with generic message; unknown email returns 401 with the same generic message (no enumeration).
- **Depends on:** BT-002, BT-005.

### BT-007: Failed-sign-in rate limiting (`ISignInRateLimiter`)
- **Implements:** L2-042.
- **Slice:** new `ISignInRateLimiter` (Application) + `SignInRateLimiter` (Infrastructure) backed by `IMemoryCache`. Tracks `email → attempts within 60 s`. `SignInUserCommand` handler calls `EnsureNotLocked(email)` first; failures call `RegisterFailedAttempt(email)`. Threshold: 5 attempts → HTTP 429 with `Retry-After: 60`.
- **Acceptance test:** `SignInRateLimitTests` — 5 wrong passwords for the same email return 401, the 6th returns 429 with `Retry-After: 60`; after the window expires, a correct password works.
- **Depends on:** BT-006.

### BT-008: `POST /api/auth/refresh`
- **Implements:** L2-006.
- **Slice:** new `RefreshAccessTokenCommand` + handler that looks up by token hash, validates not revoked / not expired, rotates (revoke old, issue new pair). Returns 401 on invalid/expired refresh.
- **Acceptance test:** `RefreshAccessTokenTests` — sign in, swap the refresh token for new pair, old one becomes unusable, new one continues to work.
- **Depends on:** BT-005, BT-006.

### BT-009: `POST /api/auth/sign-out`
- **Implements:** L2-007.
- **Slice:** new `SignOutCommand` that revokes the supplied refresh token. Audited via `AuditingBehavior` with `SIGN_OUT` event.
- **Acceptance test:** `SignOutTests` — sign in, sign out, the refresh token no longer mints new access tokens.
- **Depends on:** BT-005, BT-006.

### BT-010: `POST /api/auth/password-reset` (request link)
- **Implements:** L2-008, L2-010, L2-044.
- **Slice:** new entity `Users/PasswordResetToken` + config + migration `AddPasswordResetTokens`. `RequestPasswordResetCommand` always returns HTTP 202 with no body; if email matches a real user, generates 256-bit token, stores HMAC-SHA-256 hash (via `ITokenHasher`), dispatches email through `LoggingEmailSender`. Audits `PASSWORD_RESET_REQUESTED`.
- **Acceptance test:** `RequestPasswordResetTests` — submitting a known and unknown email both return 202 with identical timing class; only the known email writes a `PasswordResetToken` row and triggers `LoggingEmailSender`.
- **Depends on:** BT-005 (for `ITokenHasher`).

### BT-011: `POST /api/auth/password-reset/confirm` (complete reset)
- **Implements:** L2-003, L2-009, L2-010.
- **Slice:** `CompletePasswordResetCommand` validates the supplied raw token against stored hash, expiry, and unused flag; updates password; revokes **all** refresh tokens for that user; marks the reset token used; audits `PASSWORD_RESET_COMPLETED`.
- **Acceptance test:** `CompletePasswordResetTests` — request reset, complete it with the captured token, sign in with the new password; using the same token a second time returns 400; expired tokens return 400.
- **Depends on:** BT-005, BT-010.

---

## C. User profile / account (Users feature)

### BT-012: `PUT /api/users/me/profile` (edit name + display name)
- **Implements:** L2-012.
- **Slice:** `UpdateProfileCommand` + handler + validator (fullName 1–100, displayName 1–40). `UsersController.UpdateProfile`.
- **Acceptance test:** `UpdateProfileTests` — valid update → 200 + updated values reflected on `/me`; empty fullName → 400; over-length → 400.

### BT-013: Email-change request + confirmation
- **Implements:** L2-013.
- **Slice:** add `PendingEmailChange` value type on `User`; two commands — `RequestEmailChangeCommand` (requires re-typed password, stores `PendingEmail` + verification token, sends verification via `LoggingEmailSender`) and `ConfirmEmailChangeCommand` (validates token, promotes pending to primary). `UsersController.RequestEmailChange` + `ConfirmEmailChange`. Migration `AddPendingEmailChanges`.
- **Acceptance test:** `EmailChangeTests` — request with wrong current password → 401; request with right password → 202 + verification email recorded; confirm with the token → email updated; confirm with a stale token → 400.
- **Depends on:** BT-005 (for `ITokenHasher`).

### BT-014: Default notification preferences + `PUT /api/users/me/notification-preferences`
- **Implements:** L2-016, L2-017.
- **Slice:** new entity `Notifications/NotificationPreference` + config + migration `AddNotificationPreferences`. `RegisterUserCommandHandler` extended to create a default-on `NotificationPreference` in the same transaction. New `UpdateNotificationPreferencesCommand` + handler that upserts.
- **Acceptance test:** `NotificationPreferencesTests` — fresh user has all three toggles on; toggling individually persists; querying returns the latest state.

### BT-015: `DELETE /api/users/me` (account deletion)
- **Implements:** L2-014, L2-015.
- **Slice:** `DeleteAccountCommand` requires re-typed password; `User.Tombstone()` replaces identifying fields with `[deleted user]` markers, sets `DeletedAt`; cancels every hosted future event via the same `CancelEvent` handler (reused per-event in a loop, no shortcut); removes user from invitee lists on future events. Audits `ACCOUNT_DELETED`.
- **Acceptance test:** `DeleteAccountTests` — wrong password → 401; correct password → 204; subsequent `/me` with the JWT → 401 (refresh tokens revoked); past-event guest lists show `[deleted user]` while present events drop the user.
- **Depends on:** BT-018 (CancelEvent reuse).

---

## D. Events feature

### BT-016: Event entity + schema + create slice
- **Implements:** L2-018, L2-019, L2-020, L2-021.
- **Slice:** new entities `Events/Event`, `Events/EventStatus`, `Events/Invitation`, `Events/Rsvp`, `Events/RsvpStatus` (one file each) + EF configs + migration `AddEventsAndInvitations` and `AddRsvps`. New `CreateEventCommand` + handler that persists Event + initial Invitations + host self-RSVP (`Going`) in one transaction. Validator: title 1–120, location 1–200, description 0–2000, date today-or-later, time HH:MM. `EventsController.Create`.
- **Acceptance test:** `CreateEventTests` — happy path returns 201 with new event id; past date → 400 `code=PAST_DATE`; over-length title → 400; no invitees still creates event with host as sole going member.

### BT-017: `GET /api/events` (list grouped by time window) + filter chips
- **Implements:** L2-022, L2-024, L2-025.
- **Slice:** `ListMyEventsQuery` accepting scope filter (`All`/`Hosting`/`Invited`/`Past`). Handler joins Events ↔ Invitations ↔ Rsvps for the current user and projects to a flat DTO with `thisWeek`, `laterThisMonth`, `nextMonth`, `past` groupings using `AsNoTracking()`. `EventsController.List`.
- **Acceptance test:** `ListMyEventsTests` — seed events across windows; assert correct grouping; `scope=Hosting` only returns hosted; `scope=Past` returns reverse-chronological events older than today.

### BT-018: `GET /api/events/{id}` + host/invitee authorization (and `CancelEvent` reuse)
- **Implements:** L2-023, L2-027, L2-029, L2-030, L2-036.
- **Slice:** `GetEventQuery` returning host details, guest list (respecting `Event.ShowGuestList`), aggregate RSVP counts, current-user RSVP, host-actions flag. Authorization: 403 unless caller is host or has an unrevoked invitation. `CancelEventCommand` (host-only, sets `Status=Cancelled`, audits, dispatches cancellation notifications via `DispatchInvitationNotification` with kind `EventCancelled`).
- **Acceptance test:** `GetEventTests` + `CancelEventTests` — non-invited user → 403; host sees guest list + actions; invited user without `ShowGuestList` sees aggregate counts only; host cancellation flips status and produces notification log entries for every invitee with `NewInvitations=on`.
- **Depends on:** BT-001, BT-016, BT-019.

### BT-019: `DispatchInvitationNotification` + `INotificationSender`
- **Implements:** L2-028, L2-030, L2-037.
- **Slice:** new `INotificationSender` (Application) + `LoggingNotificationSender` (Infrastructure no-op). New `DispatchInvitationNotificationCommand` taking `(eventId, recipientUserId, kind)`; handler reads the recipient's `NotificationPreference`, gates on `NewInvitations`, calls `INotificationSender`. Reusable by `CreateEvent` (on each invitee), `AddInvitee`, `UpdateEvent` (date/time/location change), `CancelEvent`.
- **Acceptance test:** `DispatchInvitationNotificationTests` — recipient with `NewInvitations=off` does not receive; recipient with on does; passes the correct kind to the sender.

### BT-020: `PUT /api/events/{id}` (host edit)
- **Implements:** L2-026, L2-027, L2-028.
- **Slice:** `UpdateEventCommand` host-only via `IAuthorizationRequirement`; updates editable fields (title, date, time, location, description, options); on date/time/location change loops invitees and fans out `DispatchInvitationNotification(kind=EventUpdated)`.
- **Acceptance test:** `UpdateEventTests` — host can edit; non-host returns 403; editing date triggers notifications; editing only description does not.
- **Depends on:** BT-001, BT-016, BT-019.

### BT-021: Invitee management — `AddInvitee` / `RemoveInvitee`
- **Implements:** L2-031, L2-032, L2-033, L2-037.
- **Slice:** `AddInviteeCommand` host-only; matches existing user by email or creates email-only `Invitation`. `RemoveInviteeCommand` sets `Invitation.RemovedAt` and clears any existing `Rsvp`. Both audit-eligible.
- **Acceptance test:** `InviteeManagementTests` — host adds existing user → invitation created → notification dispatched; host adds unknown email → email-only invitation stored; host removes invitee → existing RSVP cleared; non-host attempts → 403.
- **Depends on:** BT-001, BT-016, BT-019.

### BT-022: `PUT /api/events/{id}/rsvp` (Set/Update RSVP)
- **Implements:** L2-034, L2-035, L2-036, L2-039.
- **Slice:** `SetRsvpCommand` invitee-only; upserts `Rsvp(Going/Maybe/CantGo, [Note?])`; rejects with 409 `code=EVENT_PASSED` if `StartsAtUtc < now`; dispatches `DispatchRsvpChangeNotification` (new MediatR command) gated on host's `RsvpChanges` preference.
- **Acceptance test:** `SetRsvpTests` — invitee can set; updates update aggregate counts; past event returns 409 + UI doesn't surface control (frontend concern out of backend test scope); host with `RsvpChanges=on` sees notification dispatched.
- **Depends on:** BT-001, BT-014, BT-016.

### BT-023: 24-hour event-reminder hosted service
- **Implements:** L2-038.
- **Slice:** new `Infrastructure/Reminders/EventReminderHostedService : BackgroundService` polling once per minute. New `DispatchEventRemindersCommand` finding events starting in `[24h, 24h+1min]` with attending RSVPs whose owners have `EventReminders=on`; dispatches one reminder per matching `Rsvp` and persists an idempotency row to prevent duplicates after restart.
- **Acceptance test:** `EventReminderTests` — seed an event 24 h ahead with `Going` RSVPs of mixed preferences; invoke the command; only `on` users receive; running twice does not duplicate.
- **Depends on:** BT-014, BT-016, BT-019.

---

## E. Sample-slice tasks already done by MB1

These exist in the codebase from the MVP; they ship through BT2/BI1 as **already-done** entries so the task list is exhaustive and traceability is preserved.

### BT-024: `POST /api/users` — RegisterUser (MB1)
- **Implements:** L2-001, L2-002, L2-003.
- **Status:** Done in MB1; integration tests live in `RegisterUserTests`.
- **BI1 follow-up:** extend to also create the default `NotificationPreference` (BT-014 takes ownership).

### BT-025: `GET /api/users/me` — GetCurrentUser (MB1)
- **Implements:** L2-011 (read-only profile view).
- **Status:** Done in MB1; integration tests live in `GetCurrentUserTests`.
- **BI1 follow-up:** none for this slice — read endpoint already correct.

---

## F. Sequencing and dependency graph

```
A.   BT-001 (auth behavior) ──┐
     BT-002 (auditing) ───────┼──────┐
     BT-003 (redaction)       │      │
     BT-004 (HSTS+health)     │      │
                              │      │
B.   BT-005 (refresh store) ──┴───┐  │
     BT-006 (sign-in) ───────────┘  │
     BT-007 (rate limit) ──┐        │
     BT-008 (refresh)      │        │
     BT-009 (sign-out)     │        │
     BT-010 (reset request)│        │
     BT-011 (reset confirm)        │
                                   │
C.   BT-012 (profile edit) ────────┤
     BT-013 (email change)         │
     BT-014 (prefs)                │
     BT-015 (delete) needs BT-018  │
                                   │
D.   BT-016 (event create) ────────┤
     BT-017 (event list)           │
     BT-018 (event get + cancel) needs BT-019
     BT-019 (notify dispatch)
     BT-020 (event update) needs BT-019
     BT-021 (invitees)    needs BT-019
     BT-022 (rsvp)        needs BT-014, BT-016
     BT-023 (reminders)   needs BT-014, BT-016, BT-019

E.   BT-024, BT-025  — already complete (MB1)
```

Recommended order for BI1: A → B → C → D in band order. Within each band, top-to-bottom is fine; the dependency notes above are the only hard constraints.

## G. Sizing

23 active tasks. Each is a single vertical slice (one HTTP endpoint or one cross-cutting concern), one acceptance test class, ≤2 hours of code + test typical. Cross-cutting tasks BT-001..BT-004 are the largest in scope but each adds exactly one behavior or one filter — within sizing rule.

All tasks are radically simple: no task introduces a repository, a UoW, a generic CRUD base, a `Result<T>` wrapper, or a MediatR `INotification` event bus. Each task names its acceptance test by file name and the L2 IDs it traces to.
