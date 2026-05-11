# Contributing to Pull Up

Thanks for contributing. This document explains the workflow for issues, branches, and pull requests.

## Ground rules

1. Be respectful and constructive in all interactions.
2. Keep changes focused; avoid unrelated refactors in the same PR.
3. Prefer small, reviewable pull requests over large batches.

## Development setup

### Backend

```powershell
dotnet restore PullUp.sln
dotnet build PullUp.sln -c Release
dotnet tool install --global dotnet-ef --version 10.0.*
dotnet ef database update --project backend/src/PullUp.Infrastructure --startup-project backend/src/PullUp.Api
dotnet run --project backend/src/PullUp.Api
```

### Frontend

```powershell
cd frontend
npm install
npx playwright install chromium
npm start
```

## Branch and commit guidelines

1. Create feature branches from `main`.
2. Use clear commit messages in imperative voice.
3. Keep each commit coherent and independently understandable.

## Pull request checklist

Before opening a PR, ensure:

1. The change is scoped to one concern.
2. Documentation is updated when behavior or setup changes.
3. Relevant tests pass locally:

```powershell
dotnet test PullUp.sln -c Release
cd frontend
npm test
npm run e2e
```

4. The PR description explains:
   - What changed
   - Why it changed
   - Any migration or rollout considerations

## Reporting bugs

Open a GitHub issue and include:

1. Repro steps
2. Expected behavior
3. Actual behavior
4. Environment details (`dotnet --info`, `node --version`, browser)

## Suggesting enhancements

Open a feature request issue describing:

1. Problem statement
2. Proposed solution
3. Alternatives considered

## Security issues

Do **not** open public issues for vulnerabilities. Follow [SECURITY.md](SECURITY.md).
