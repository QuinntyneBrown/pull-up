# Pull Up

Pull Up is a full-stack, clean-architecture family event management application with a .NET API backend and an Angular frontend.

It is designed as a reference-quality codebase for vertical-slice backend features, interface-driven frontend service consumption, and practical end-to-end workflow coverage.

## Why this project

- **Backend:** Clean Architecture with MediatR, FluentValidation, EF Core 10, and JWT authentication.
- **Frontend:** Angular 21 standalone app + libraries (`api`, `components`, `domain`) with Material 3 theming.
- **Testing:** Backend integration tests and frontend Playwright acceptance tests for key user flows.
- **Developer-first docs:** Runbooks, plans, and specs under `docs/`.

## Repository structure

```text
.
|- backend/          # .NET API, application layers, infrastructure, tests
|- frontend/         # Angular workspace (app + libraries + e2e tests)
|- docs/             # specs, runbooks, plans, evaluations
|- PullUp.sln        # backend solution entrypoint
|- global.json       # .NET SDK pin
```

## Tech stack

| Area | Technology |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core 10, SQL Server Express |
| Auth | JWT Bearer (`JsonWebTokenHandler`) |
| Frontend | Angular 21, Angular Material (M3), RxJS |
| Testing | xUnit integration tests, Playwright E2E, Vitest (Angular unit tests) |

## Quick start

### Prerequisites

1. .NET 10 SDK (`global.json` pins `10.0.101`, latest 10.x feature roll-forward enabled).
2. SQL Server Express named instance: `SQLEXPRESS`.
3. Node.js 22+ and npm 10+.

### 1) Backend

```powershell
dotnet restore PullUp.sln
dotnet build PullUp.sln -c Release
dotnet tool install --global dotnet-ef --version 10.0.*
dotnet ef database update --project backend/src/PullUp.Infrastructure --startup-project backend/src/PullUp.Api
dotnet run --project backend/src/PullUp.Api
```

API defaults:
- `http://localhost:5080`
- `https://localhost:5443`
- OpenAPI in Development: `/openapi/v1.json`

### 2) Frontend

```powershell
cd frontend
npm install
npx playwright install chromium
npm start
```

App defaults:
- `http://localhost:4200`

## Run tests

### Backend integration tests

```powershell
dotnet test PullUp.sln -c Release
```

### Frontend tests

```powershell
cd frontend
npm test
npm run e2e
```

## Architecture highlights

- **Backend dependency direction:** `Api -> Application + Infrastructure`, `Infrastructure -> Application + Domain`, `Application -> Domain`, `Domain -> none`.
- **Vertical slices:** handlers, validators, requests/responses grouped by feature.
- **No repository abstraction layer:** handlers depend on `IAppDbContext`.
- **Frontend layering:** app consumes libraries through path aliases and injection tokens, avoiding direct dependence on concrete service classes in consuming libraries.

## Documentation

- Product idea: `docs/idea.md`
- Backend runbook: `docs/runbooks/backend.md`
- Frontend runbook: `docs/runbooks/frontend.md`
- Specifications: `docs/specs/`

## Security

Please review [SECURITY.md](SECURITY.md) before reporting vulnerabilities.

## Contributing

Please review [CONTRIBUTING.md](CONTRIBUTING.md) before opening pull requests.

## License

Licensed under the [MIT License](LICENSE).
