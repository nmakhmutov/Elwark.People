# CI / Docker / Test Fix Design

**Date:** 2026-04-02
**Status:** Approved

## Overview

Three coordinated changes following a major refactoring that extracted `People.Application` from `People.Api`:

1. Modernise GitHub Actions (single unified workflow with tests + coverage)
2. Fix Dockerfiles (add missing project, fix wrong reference, simplify COPY pattern)
3. Fix all broken tests (namespace/type-name mismatches from the refactoring)

---

## Task 1: GitHub Actions

### What changes

- **Delete** `.github/workflows/people.yml`
- **Delete** `.github/workflows/people.worker.yml`
- **Delete** `.github/workflows/composite/build/action.yml`
- **Create** `.github/workflows/people.yml` (new unified file)
- **Create** `coverage.runsettings` at repo root

### Workflow structure

Triggers: `push` and `pull_request` on paths `src/**`, `tests/**`, `.github/workflows/people.yml`.
Concurrency: cancel-in-progress per `${{ github.workflow }}-${{ github.ref }}`.

**Job: `build-docker-images`**
Matrix over `api` and `worker`. Uses `docker/setup-buildx-action` + `docker/build-push-action` with GitHub Actions layer cache (`type=gha`). Saves image tarballs as artifacts (retention 1 day) for potential downstream use.

**Job: `unit-tests`**
Checks out, sets up .NET 10, caches NuGet (keyed on `Directory.Packages.props`), builds the full solution (`Elwark.People.slnx`), then runs `People.UnitTests` with XPlat code coverage and `coverage.runsettings`. Uploads coverage XML as artifact.

**Job: `integration-tests`**
Same setup as unit tests. Sets `Serilog` env vars to reduce noise (Warning level). Runs `People.IntegrationTests` (uses Testcontainers — Docker is available on `ubuntu-latest`). Uploads coverage XML (`if-no-files-found: ignore` since integration coverage is optional).

**Job: `coverage-report`**
`needs: [unit-tests, integration-tests]`, runs `if: ${{ !cancelled() }}` so it always produces a report even if one test job fails. Downloads all `coverage-*` artifacts, runs `danielpalme/ReportGenerator-GitHub-Action@5.5.4`, appends markdown summary to `$GITHUB_STEP_SUMMARY`, uploads HTML report artifact.

### coverage.runsettings

Minimal file enabling XPlat coverage collection. Excludes test assemblies from coverage.

---

## Task 2: Dockerfiles

### Dependency graph (drives restore layer)

```
People.Api     → People.Infrastructure → People.Application → People.Domain
People.Worker  → People.Infrastructure → People.Application → People.Domain
```

### What changes

**`src/People.Api/Dockerfile`**
- Add `People.Application` to restore layer (was missing — new project from refactoring)
- Switch from `COPY "src/X/X.csproj" "src/X/"` to `COPY src/X/*.csproj src/X/` (wildcard, less brittle)
- Two-stage restore optimization is preserved

**`src/People.Worker/Dockerfile`**
- Remove `People.Api.csproj` from restore layer (Worker does not depend on Api)
- Add `People.Application` to restore layer (was missing)
- Same wildcard COPY pattern

**`docker-compose.yml` / `docker-compose.override.yml`** — no changes.

---

## Task 3: Test Fixes

### Root cause

The refactoring moved command handlers, providers, and services from `People.Api.Application.*` / `People.Api.Infrastructure.*` into the new `People.Application` project, but test files were not updated.

### Namespace corrections (all files)

| Old import | New import |
|---|---|
| `People.Api.Application.*` | `People.Application.*` |
| `People.Api.Infrastructure.Providers.*` | `People.Application.Providers.*` |
| `People.Infrastructure.Outbox.Mappers` | `People.Infrastructure.Mappers` |

### Type name corrections

| File | Old | New |
|---|---|---|
| `PostgreSqlFixture.cs` | `OutboxSaveChangesPipeline<T>` | `OutboxPipeline<T>` |
| `OutboxMessageTests.cs` | `OutboxStatus.Success` | `OutboxStatus.Completed` |

### Files to update

**Integration test fixtures:**
- `tests/Integration/People.IntegrationTests/Commands/CommandTestFixture.cs`
- `tests/Integration/People.IntegrationTests/EventHandlers/IntegrationEventHandlerTestFixture.cs`
- `tests/Integration/People.IntegrationTests/Infrastructure/PostgreSqlFixture.cs`

**New integration test files (broken imports):**
- `tests/Integration/People.IntegrationTests/Commands/SendWebhooksCommandTests.cs`
- `tests/Integration/People.IntegrationTests/Commands/UpdateLastActivityCommandTests.cs`
- `tests/Integration/People.IntegrationTests/Commands/UpdateLastLoginCommandTests.cs`

**New unit test files:**
- `tests/Unit/People.UnitTests/Application/Commands/EnrichAccountCommandTests.cs`
- `tests/Unit/People.UnitTests/Application/EventHandlers/EnrichAccountCommandHandlerTests.cs`
- `tests/Unit/People.UnitTests/Outbox/OutboxMessageTests.cs`

**Other modified test files** (check and fix any remaining `People.Api.*` imports):
- All files listed as modified in git status under `tests/`

### Approach

Verify actual namespaces in source before fixing each file (grep the source for the correct namespace). Fix imports only — no logic changes.

---

## Out of scope

- Smoke tests (no multi-service DB infrastructure like Spendly)
- docker-compose.override.yml changes
- Any logic changes to application or domain code
