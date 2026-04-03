# Integration Test Split Design

**Date:** 2026-04-03
**Status:** Approved

## Goal

Split the single `People.IntegrationTests` project into three projects:
- `People.IntegrationTests.Shared` — shared test infrastructure (no tests)
- `People.Api.IntegrationTests` — API integration tests
- `People.Worker.IntegrationTests` — Worker integration tests

Run API and Worker integration tests as parallel jobs in GitHub Actions.

## Folder Structure

```
tests/Integration/
  People.IntegrationTests.Shared/          ← plain class library, no tests
    Infrastructure/
      PostgreSqlFixture.cs
      IntegrationDatabaseCleanup.cs
      AccountTestFactory.cs
      AccountPrivateState.cs
      NoOpMediator.cs
    People.IntegrationTests.Shared.csproj

  People.Api.IntegrationTests/             ← xUnit test project
    Commands/                              (CommandTestFixture + all command flow tests)
    Endpoints/
    EventHandlers/                         (IntegrationEventHandlerTestFixture + tests)
    Grpc/
    Infrastructure/                        (ConfirmationServiceTests, AccountRepositoryTests,
                                            EntityConfigurationTests, PeopleDbContextTests)
    Queries/                               (QueryIntegrationTestBase + tests)
    Web/                                   (PeopleApiFactory, JwtTestTokens, RestApiTestBase)
    Infrastructure/PostgresCollection.cs   (xUnit [CollectionDefinition] — assembly-scoped)
    GlobalUsings.cs
    People.Api.IntegrationTests.csproj

  People.Worker.IntegrationTests/          ← xUnit test project
    Outbox/                                (OutboxDispatchJobTests, OutboxCleanupJobTests,
                                            OutboxSaveChangesPipelineTests)
    Infrastructure/PostgresCollection.cs   (xUnit [CollectionDefinition] — assembly-scoped)
    GlobalUsings.cs
    People.Worker.IntegrationTests.csproj
```

> `PostgresCollection` must exist in each test project because xUnit `[CollectionDefinition]` is assembly-scoped. Each is a one-liner referencing `PostgreSqlFixture` from Shared.

## Project References

### `People.IntegrationTests.Shared.csproj` — class library (`Microsoft.NET.Sdk`)
- `<FrameworkReference Include="Microsoft.AspNetCore.App"/>`
- Packages: `Testcontainers.PostgreSql`, `xunit` (for `IAsyncLifetime`), `NSubstitute`
- Project refs: `People.Infrastructure`, `People.Domain`

### `People.Api.IntegrationTests.csproj` — test project
- `<FrameworkReference Include="Microsoft.AspNetCore.App"/>`
- Packages: `Mediator.Abstractions`, `Mediator.SourceGenerator`, `Microsoft.AspNetCore.TestHost`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.NET.Test.Sdk`, `NSubstitute`, `xunit`, `xunit.runner.visualstudio`
- Project refs: `People.Api`, `People.Infrastructure`, `People.IntegrationTests.Shared`

### `People.Worker.IntegrationTests.csproj` — test project
- `<FrameworkReference Include="Microsoft.AspNetCore.App"/>`
- Packages: `Microsoft.NET.Test.Sdk`, `NSubstitute`, `Quartz`, `xunit`, `xunit.runner.visualstudio`
- Project refs: `People.Infrastructure`, `People.Worker` (with `Aliases="PeopleWorker"`), `People.IntegrationTests.Shared`

## GitHub Actions

Replace the single `integration-tests` job with two parallel jobs:

```yaml
api-integration-tests:
  name: API Integration Tests
  runs-on: ubuntu-latest
  env:
    DOTNET_NOLOGO: true
    NUGET_XMLDOC_MODE: skip
    Serilog__MinimumLevel__Default: Warning
    Serilog__MinimumLevel__Override__Microsoft: Warning
    Serilog__MinimumLevel__Override__Microsoft.Hosting: Information
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: 10.0.x }
    - uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: nuget-${{ runner.os }}-${{ hashFiles('**/Directory.Packages.props') }}
        restore-keys: nuget-${{ runner.os }}-
    - run: dotnet build Elwark.People.slnx --configuration Release --verbosity minimal
    - run: dotnet test tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj
        --configuration Release --no-build --no-restore --verbosity minimal
        --collect:"XPlat Code Coverage" --settings coverage.runsettings
        --results-directory TestResults
    - uses: actions/upload-artifact@v6
      with:
        name: coverage-api-integration
        path: TestResults/**/coverage.cobertura.xml
        if-no-files-found: error
        retention-days: 1

worker-integration-tests:
  name: Worker Integration Tests
  runs-on: ubuntu-latest
  env:
    DOTNET_NOLOGO: true
    NUGET_XMLDOC_MODE: skip
    Serilog__MinimumLevel__Default: Warning
    Serilog__MinimumLevel__Override__Microsoft: Warning
    Serilog__MinimumLevel__Override__Microsoft.Hosting: Information
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: 10.0.x }
    - uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: nuget-${{ runner.os }}-${{ hashFiles('**/Directory.Packages.props') }}
        restore-keys: nuget-${{ runner.os }}-
    - run: dotnet build Elwark.People.slnx --configuration Release --verbosity minimal
    - run: dotnet test tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj
        --configuration Release --no-build --no-restore --verbosity minimal
        --collect:"XPlat Code Coverage" --settings coverage.runsettings
        --results-directory TestResults
    - uses: actions/upload-artifact@v6
      with:
        name: coverage-worker-integration
        path: TestResults/**/coverage.cobertura.xml
        if-no-files-found: error
        retention-days: 1
```

The `coverage-report` job updates its `needs` list:

```yaml
coverage-report:
  needs: [ unit-tests, api-integration-tests, worker-integration-tests ]
```

The existing `pattern: coverage-*` already matches both new artifact names — no further changes needed.

## Solution File

`Elwark.People.slnx` replaces the old integration test project with the three new ones:

```xml
<Folder Name="/tests/">
    <Project Path="tests/Unit/People.UnitTests/People.UnitTests.csproj"/>
    <Project Path="tests/Integration/People.IntegrationTests.Shared/People.IntegrationTests.Shared.csproj"/>
    <Project Path="tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj"/>
    <Project Path="tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj"/>
</Folder>
```

The old `tests/Integration/People.IntegrationTests/` directory is deleted.

## Namespace Changes

| Old namespace | New namespace |
|---|---|
| `People.IntegrationTests.Infrastructure` | `People.IntegrationTests.Shared.Infrastructure` (shared project) |
| `People.IntegrationTests.Commands` | `People.Api.IntegrationTests.Commands` |
| `People.IntegrationTests.Endpoints` | `People.Api.IntegrationTests.Endpoints` |
| `People.IntegrationTests.EventHandlers` | `People.Api.IntegrationTests.EventHandlers` |
| `People.IntegrationTests.Grpc` | `People.Api.IntegrationTests.Grpc` |
| `People.IntegrationTests.Queries` | `People.Api.IntegrationTests.Queries` |
| `People.IntegrationTests.Web` | `People.Api.IntegrationTests.Web` |
| `People.IntegrationTests.Outbox` | `People.Worker.IntegrationTests.Outbox` |
