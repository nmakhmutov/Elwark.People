# Health Checks Design

**Date:** 2026-04-03

**Goal:** Add liveness and readiness endpoints to the API and worker, then add a GitHub Actions smoke test job that runs the built Docker images and verifies those endpoints return HTTP 200.

## Runtime Design

### API

`src/People.Api/Program.cs` will register ASP.NET Core health checks and map:

- `/health/live` with `Predicate = static _ => false`
- `/health/ready` with `Predicate = static _ => true`

Both endpoints will serialize the `HealthReport` directly with `WriteAsJsonAsync(report)`.

Readiness is intentionally startup-only for this change. No dependency-specific checks are added yet.

### Worker

`src/People.Worker/Program.cs` will expose the same two health endpoints over HTTP.

The worker currently runs as a generic host without an HTTP surface. To support health probes, it will add a lightweight ASP.NET Core pipeline and listen on a configurable health URL. The default container-facing port will be `8081`.

The worker readiness contract is also startup-only. Once the process has started successfully, both health routes can return healthy.

## Container Design

The Docker images remain the deployment artifact under test.

No response-body assertions are required. Smoke tests only need HTTP 200 from:

- API: `/health/live`, `/health/ready`
- Worker: `/health/live`, `/health/ready`

Startup currently includes database migrations, so smoke checks must poll with a timeout instead of assuming immediate availability.

## CI Design

The existing `build-docker-images` job in `.github/workflows/people.yml` will continue building both images and will additionally upload each generated Docker tarball as an artifact.

A new `smoke-tests` job will:

1. Download both Docker image artifacts
2. Load them with `docker load`
3. Start each container with the minimum environment and port mappings needed for startup
4. Poll the health endpoints until they return HTTP 200 or the timeout is reached
5. Fail the workflow if either container never becomes healthy

The smoke test validates the same images produced by the earlier build step instead of rebuilding them.

## Testing Strategy

This change follows TDD:

- Add API integration coverage for the two health endpoints before implementation
- Add worker integration coverage for the two health endpoints before implementation
- Run each new test first to confirm the expected failure
- Implement the runtime changes minimally until tests pass
- Extend the workflow and verify the smoke-test job wiring

## Scope Boundaries

This change does not include:

- Dependency-tagged readiness checks
- Dockerfile `HEALTHCHECK` instructions
- Full response-body validation in CI
- Compose-driven smoke orchestration
