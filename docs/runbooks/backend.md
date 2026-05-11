# Backend runbook — Pull Up API

The backend is a Clean-Architecture .NET 10 ASP.NET Core API with MediatR (free, 12.5.x), FluentValidation, EF Core 10 against Microsoft SQL Server, and JWT Bearer authentication. It lives entirely under `./backend/`. This runbook is for developers running it on a workstation.

## Prerequisites (one-time)

1. **.NET 10 SDK.** Confirm with `dotnet --list-sdks`; the repo's `global.json` pins `10.0.101` with `rollForward: latestFeature` so any 10.x SDK works.
2. **SQL Server Express (SQLEXPRESS).** Install from the Microsoft SQL Server download page; choose the *Express* edition and accept the default named instance `SQLEXPRESS`. This is the only supported local database — do **not** substitute LocalDB, an in-memory provider, or a Docker SQL Server.
3. **EF Core CLI tools.** Once globally: `dotnet tool install --global dotnet-ef --version 10.0.*`. Confirm with `dotnet ef --version`.

The local connection string is committed in `backend/src/PullUp.Api/appsettings.Development.json`:

```
Server=.\SQLEXPRESS;Database=PullUp;Trusted_Connection=True;TrustServerCertificate=True;
```

The JWT signing key is a 64-byte development-only value in the same file; replace via user-secrets or environment variables before deploying anywhere outside your workstation.

## First-time setup

From the repo root:

```
dotnet restore backend\PullUp.sln
dotnet build backend\PullUp.sln -c Release
dotnet ef database update --project backend/src/PullUp.Infrastructure --startup-project backend/src/PullUp.Api
```

The migration creates a `PullUp` database with a single `Users` table (Id, Email, FullName, DisplayName, PasswordHash, Role, CreatedAt) and a unique index on `Email`.

## Run the API

```
dotnet run --project backend/src/PullUp.Api
```

The API listens on `http://localhost:5080` (`https://localhost:5443` over TLS) using the `Development` profile in `Properties/launchSettings.json`. The OpenAPI document is exposed at `/openapi/v1.json` in Development.

## Sample slice (the MVP reference flow)

The sample feature is User Registration plus authenticated profile read. It is one full vertical slice:

- `Features/Users/RegisterUser/RegisterUserCommand` + `Handler` + `Validator` (FluentValidation) + `Response` — feature-folder layout under `Application/Features/Users/RegisterUser/`.
- `Features/Users/GetCurrentUser/GetCurrentUserQuery` + `Handler` + `Response`.
- `Controllers/UsersController` exposes `POST /api/users` (anonymous) and `GET /api/users/me` (JWT-authorized).
- `Persistence/AppDbContext` implements `Application.Abstractions.IAppDbContext`; handlers depend only on `IAppDbContext`.

### Curl walkthrough

Register a user — round-trips HTTP → MediatR → FluentValidation → handler → EF → SQLEXPRESS, then returns a JWT:

```
curl -k -X POST https://localhost:5443/api/users \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Rosa Marquez","email":"rosa@example.com","password":"Hunter2!secret"}'
```

Response (HTTP 201):

```json
{
  "userId": "…",
  "email": "rosa@example.com",
  "fullName": "Rosa Marquez",
  "displayName": "Rosa",
  "accessToken": "eyJhbGciOi…",
  "accessTokenExpiresAt": "…"
}
```

Fetch the current user with that token — round-trips through JWT bearer validation → MediatR → handler → EF:

```
TOKEN=eyJhbGciOi…
curl -k https://localhost:5443/api/users/me -H "Authorization: Bearer $TOKEN"
```

Validation failures return HTTP 400 with an RFC 7807 `ValidationProblemDetails` body whose field keys match the command property names. Duplicate email returns HTTP 409. Invalid/missing tokens on protected endpoints return HTTP 401.

## Layered pattern (what depends on what)

```
PullUp.Api            → references Application + Infrastructure
PullUp.Infrastructure → references Application + Domain
PullUp.Application    → references Domain only
PullUp.Domain         → no references
```

Concrete rules:

- Handlers live under `PullUp.Application/Features/<Feature>/<Slice>/` and depend on `IAppDbContext`, never on `AppDbContext` directly, and never on a `Repository` (there are no repositories — that is intentional).
- One C# type per file across the whole backend.
- Validators are colocated with their commands in the same feature folder and registered automatically by assembly scan.
- A `ValidationBehavior<TRequest,TResponse>` runs each command's `AbstractValidator<TCommand>` before the handler; failures throw `FluentValidation.ValidationException`, which `Program.cs` maps to HTTP 400.
- Passwords are hashed by `Pbkdf2PasswordHasher` (PBKDF2-HMAC-SHA256, 600,000 iterations, 16-byte salt, 32-byte hash) — see L2-040 in `./docs/specs/L2.md`.
- JWTs are issued and validated using `Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler`; signing uses HMAC-SHA256 against `Jwt:SigningKey`. The modern handler is chosen over `System.IdentityModel.Tokens.Jwt` to avoid pulling the legacy XML signature library transitively.

## Common tasks

- **Add a new migration:** `dotnet ef migrations add <Name> --project backend/src/PullUp.Infrastructure --startup-project backend/src/PullUp.Api`.
- **Apply migrations:** `dotnet ef database update --project backend/src/PullUp.Infrastructure --startup-project backend/src/PullUp.Api`.
- **Reset the dev database:** `dotnet ef database drop --project backend/src/PullUp.Infrastructure --startup-project backend/src/PullUp.Api -f` then `database update`.

## Troubleshooting

- *Cannot connect to SQLEXPRESS.* Verify the SQL Server (SQLEXPRESS) service is running in `services.msc`. Check the connection string uses `Server=.\SQLEXPRESS`. If TCP is enabled but the named-pipe connection fails, change the prefix to `Server=(localdb)\…` — **no**, do not. Stay on SQLEXPRESS as required by the implementation guidance; instead, enable Shared Memory and Named Pipes in SQL Server Configuration Manager.
- *401 on `/api/users/me`.* Confirm the `Authorization: Bearer <token>` header is present and the token has not expired (default lifetime 60 minutes); validate the `Jwt:SigningKey` in `appsettings.Development.json` matches what issued the token.
- *400 with `ValidationProblemDetails`.* The error map's keys are the command property names; the client should render each value array as supporting text under the corresponding input.
