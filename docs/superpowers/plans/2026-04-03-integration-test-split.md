# Integration Test Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split `People.IntegrationTests` into `People.IntegrationTests.Shared` (class library), `People.Api.IntegrationTests` (test project), and `People.Worker.IntegrationTests` (test project), with parallel CI jobs.

**Architecture:** Shared is a plain `Microsoft.NET.Sdk` class library holding fixtures and helpers with no test runner. Both test projects reference it as a `ProjectReference`. `PostgresCollection` is redefined as a one-liner in each test project because xUnit `[CollectionDefinition]` is assembly-scoped. All shared infrastructure types are `public` since they cross assembly boundaries.

**Tech Stack:** .NET 10, xUnit 2.9, Testcontainers.PostgreSql, NSubstitute, Quartz, GitHub Actions

---

### Task 1: Create People.IntegrationTests.Shared project

**Files:**
- Create: `tests/Integration/People.IntegrationTests.Shared/People.IntegrationTests.Shared.csproj`
- Create: `tests/Integration/People.IntegrationTests.Shared/Infrastructure/PostgreSqlFixture.cs`
- Create: `tests/Integration/People.IntegrationTests.Shared/Infrastructure/IntegrationDatabaseCleanup.cs`
- Create: `tests/Integration/People.IntegrationTests.Shared/Infrastructure/AccountTestFactory.cs`
- Create: `tests/Integration/People.IntegrationTests.Shared/Infrastructure/AccountPrivateState.cs`
- Create: `tests/Integration/People.IntegrationTests.Shared/Infrastructure/NoOpMediator.cs`

- [ ] **Step 1: Create project file**

```xml
<!-- tests/Integration/People.IntegrationTests.Shared/People.IntegrationTests.Shared.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App"/>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="NSubstitute"/>
    <PackageReference Include="Testcontainers.PostgreSql"/>
    <PackageReference Include="xunit"/>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../src/People.Domain/People.Domain.csproj"/>
    <ProjectReference Include="../../../src/People.Infrastructure/People.Infrastructure.csproj"/>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create PostgreSqlFixture.cs**

Copy from `tests/Integration/People.IntegrationTests/Infrastructure/PostgreSqlFixture.cs`, changing only the namespace:

```csharp
using Microsoft.EntityFrameworkCore;
using People.Infrastructure;
using People.Infrastructure.Mappers;
using People.Infrastructure.Outbox;
using Testcontainers.PostgreSql;
using Xunit;

namespace People.IntegrationTests.Shared.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    public string ConnectionString =>
        _container.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not started.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public PeopleDbContext CreateContext(TimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseNpgsql(ConnectionString, x => x.ConfigureDataSource(builder => builder.EnableDynamicJson()))
            .Options;

        var pipeline = new OutboxPipeline<PeopleDbContext>(
            new OutboxMapperRegistry<PeopleDbContext>()
                .AddMapper(new AccountCreatedMapper())
                .AddMapper(new AccountUpdatedMapper())
                .AddMapper(new AccountDeletedMapper())
        );

        return new PeopleDbContext(options, pipeline, timeProvider ?? TimeProvider.System);
    }

    public async Task DisposeAsync() =>
        await _container.DisposeAsync();
}
```

- [ ] **Step 3: Create IntegrationDatabaseCleanup.cs**

Copy from old project, changing namespace and `internal` → `public`:

```csharp
using Microsoft.EntityFrameworkCore;
using People.Infrastructure;

namespace People.IntegrationTests.Shared.Infrastructure;

public static class IntegrationDatabaseCleanup
{
    /// <summary>Removes all application rows (no FK from confirmations to accounts in migrations).</summary>
    public static async Task DeleteAllAsync(PeopleDbContext ctx, CancellationToken ct = default)
    {
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM confirmations;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM emails;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM connections;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM accounts;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM outbox_consumers;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM outbox_messages;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM webhooks;", ct);
    }
}
```

- [ ] **Step 4: Create AccountTestFactory.cs**

Copy from old project, changing namespace and `internal` → `public`:

```csharp
using System.Net;
using NSubstitute;
using People.Domain.Entities;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;

namespace People.IntegrationTests.Shared.Infrastructure;

public static class AccountTestFactory
{
    public static TimeProvider FixedUtc(DateTime utc)
    {
        var tp = Substitute.For<TimeProvider>();
        tp.GetUtcNow().Returns(new DateTimeOffset(utc, TimeSpan.Zero));
        return tp;
    }

    public static Account CreateNewAccount(IIpHasher hasher, TimeProvider clock, string nickname = "integration")
    {
        var account = Account.Create(nickname, Language.Parse("en"), IPAddress.Loopback, hasher, clock);
        account.ClearDomainEvents();
        return account;
    }

    public static IIpHasher FakeIpHasher()
    {
        var hasher = Substitute.For<IIpHasher>();
        hasher.CreateHash(Arg.Any<IPAddress>()).Returns([1, 2, 3, 4]);
        return hasher;
    }
}
```

- [ ] **Step 5: Create AccountPrivateState.cs**

Copy from old project, changing namespace and `internal` → `public`:

```csharp
using System.Reflection;
using People.Domain.Entities;
using People.Domain.ValueObjects;

namespace People.IntegrationTests.Shared.Infrastructure;

public static class AccountPrivateState
{
    private static object? GetField(Account account, string name) =>
        typeof(Account).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(account);

    public static DateTime LastLogIn(Account account) =>
        (DateTime)GetField(account, "_lastLogIn")!;

    public static DateTime UpdatedAt(Account account) =>
        (DateTime)GetField(account, "_updatedAt")!;

    public static DateTime CreatedAt(Account account) =>
        (DateTime)GetField(account, "_createdAt")!;

    public static string[] Roles(Account account) =>
        (string[])GetField(account, "_roles")!;

    public static Ban? Ban(Account account) =>
        (Ban?)GetField(account, "_ban");
}
```

- [ ] **Step 6: Create NoOpMediator.cs**

Copy from old project, changing namespace and `internal` → `public`:

```csharp
using System.Runtime.CompilerServices;
using Mediator;

namespace People.IntegrationTests.Shared.Infrastructure;

/// <summary>Minimal <see cref="IMediator"/> for integration tests that must not publish or send.</summary>
public sealed class NoOpMediator : IMediator
{
    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public async IAsyncEnumerable<object?> CreateStream(
        object request,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamQuery<TResponse> query,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamCommand<TResponse> command,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken ct)
        where TNotification : INotification =>
        ValueTask.CompletedTask;

    public ValueTask Publish(object notification, CancellationToken ct) =>
        ValueTask.CompletedTask;

    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct) =>
        ValueTask.FromResult<TResponse>(default!);

    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct) =>
        ValueTask.FromResult<TResponse>(default!);

    public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct) =>
        ValueTask.FromResult<TResponse>(default!);

    public ValueTask<object?> Send(object request, CancellationToken ct) =>
        ValueTask.FromResult<object?>(null);
}
```

- [ ] **Step 7: Build Shared project to verify it compiles**

```bash
dotnet build tests/Integration/People.IntegrationTests.Shared/People.IntegrationTests.Shared.csproj --verbosity minimal
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add tests/Integration/People.IntegrationTests.Shared/
git commit -m "Add People.IntegrationTests.Shared class library with test infrastructure"
```

---

### Task 2: Create People.Worker.IntegrationTests project

**Files:**
- Create: `tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj`
- Create: `tests/Integration/People.Worker.IntegrationTests/GlobalUsings.cs`
- Create: `tests/Integration/People.Worker.IntegrationTests/Infrastructure/PostgresCollection.cs`
- Copy + update namespace: `Outbox/OutboxDispatchJobTests.cs`
- Copy + update namespace: `Outbox/OutboxCleanupJobTests.cs`
- Copy + update namespace: `Outbox/OutboxSaveChangesPipelineTests.cs`

- [ ] **Step 1: Create project file**

```xml
<!-- tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App"/>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk"/>
    <PackageReference Include="NSubstitute"/>
    <PackageReference Include="Quartz"/>
    <PackageReference Include="xunit"/>
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../src/People.Infrastructure/People.Infrastructure.csproj"/>
    <ProjectReference Include="../../../src/People.Worker/People.Worker.csproj" Aliases="PeopleWorker"/>
    <ProjectReference Include="../People.IntegrationTests.Shared/People.IntegrationTests.Shared.csproj"/>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create GlobalUsings.cs**

```csharp
global using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 3: Create Infrastructure/PostgresCollection.cs**

```csharp
using People.IntegrationTests.Shared.Infrastructure;
using Xunit;

namespace People.Worker.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgreSqlFixture>;
```

- [ ] **Step 4: Copy Outbox files from old project**

```bash
cp -r tests/Integration/People.IntegrationTests/Outbox/ tests/Integration/People.Worker.IntegrationTests/Outbox/
```

- [ ] **Step 5: Update namespace declarations in Outbox files**

```bash
find tests/Integration/People.Worker.IntegrationTests/Outbox -name "*.cs" -exec \
  sed -i '' 's/namespace People\.IntegrationTests\.Outbox;/namespace People.Worker.IntegrationTests.Outbox;/g' {} +
```

- [ ] **Step 6: Update using directives in Outbox files**

```bash
find tests/Integration/People.Worker.IntegrationTests/Outbox -name "*.cs" -exec \
  sed -i '' 's/using People\.IntegrationTests\.Infrastructure;/using People.IntegrationTests.Shared.Infrastructure;/g' {} +
```

- [ ] **Step 7: Update fully-qualified Infrastructure references (used in type aliases)**

```bash
find tests/Integration/People.Worker.IntegrationTests/Outbox -name "*.cs" -exec \
  sed -i '' 's/People\.IntegrationTests\.Infrastructure\./People.IntegrationTests.Shared.Infrastructure./g' {} +
```

- [ ] **Step 8: Build Worker test project to verify it compiles**

```bash
dotnet build tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj --verbosity minimal
```

Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add tests/Integration/People.Worker.IntegrationTests/
git commit -m "Add People.Worker.IntegrationTests project with Outbox job tests"
```

---

### Task 3: Create People.Api.IntegrationTests project

**Files:**
- Create: `tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj`
- Create: `tests/Integration/People.Api.IntegrationTests/GlobalUsings.cs`
- Create: `tests/Integration/People.Api.IntegrationTests/Infrastructure/PostgresCollection.cs`
- Copy + update: all files from `Commands/`, `Endpoints/`, `EventHandlers/`, `Grpc/`, `Queries/`, `Web/`
- Copy + update: test files only from `Infrastructure/` (ConfirmationServiceTests, AccountRepositoryTests, EntityConfigurationTests, PeopleDbContextTests)

- [ ] **Step 1: Create project file**

```xml
<!-- tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App"/>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mediator.Abstractions"/>
    <PackageReference Include="Mediator.SourceGenerator">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing"/>
    <PackageReference Include="Microsoft.AspNetCore.TestHost"/>
    <PackageReference Include="Microsoft.NET.Test.Sdk"/>
    <PackageReference Include="NSubstitute"/>
    <PackageReference Include="xunit"/>
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../src/People.Api/People.Api.csproj"/>
    <ProjectReference Include="../../../src/People.Infrastructure/People.Infrastructure.csproj"/>
    <ProjectReference Include="../People.IntegrationTests.Shared/People.IntegrationTests.Shared.csproj"/>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create GlobalUsings.cs**

```csharp
global using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 3: Create Infrastructure/PostgresCollection.cs**

```csharp
using People.IntegrationTests.Shared.Infrastructure;
using Xunit;

namespace People.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgreSqlFixture>;
```

- [ ] **Step 4: Copy all source folders from old project**

```bash
cp -r tests/Integration/People.IntegrationTests/Commands/    tests/Integration/People.Api.IntegrationTests/Commands/
cp -r tests/Integration/People.IntegrationTests/Endpoints/   tests/Integration/People.Api.IntegrationTests/Endpoints/
cp -r tests/Integration/People.IntegrationTests/EventHandlers/ tests/Integration/People.Api.IntegrationTests/EventHandlers/
cp -r tests/Integration/People.IntegrationTests/Grpc/        tests/Integration/People.Api.IntegrationTests/Grpc/
cp -r tests/Integration/People.IntegrationTests/Queries/     tests/Integration/People.Api.IntegrationTests/Queries/
cp -r tests/Integration/People.IntegrationTests/Web/         tests/Integration/People.Api.IntegrationTests/Web/
```

- [ ] **Step 5: Copy only the test files from old Infrastructure/**

```bash
cp tests/Integration/People.IntegrationTests/Infrastructure/ConfirmationServiceTests.cs   tests/Integration/People.Api.IntegrationTests/Infrastructure/
cp tests/Integration/People.IntegrationTests/Infrastructure/AccountRepositoryTests.cs     tests/Integration/People.Api.IntegrationTests/Infrastructure/
cp tests/Integration/People.IntegrationTests/Infrastructure/EntityConfigurationTests.cs   tests/Integration/People.Api.IntegrationTests/Infrastructure/
cp tests/Integration/People.IntegrationTests/Infrastructure/PeopleDbContextTests.cs       tests/Integration/People.Api.IntegrationTests/Infrastructure/
```

- [ ] **Step 6: Update all namespace declarations**

```bash
find tests/Integration/People.Api.IntegrationTests -name "*.cs" -exec \
  sed -i '' \
    -e 's/^namespace People\.IntegrationTests\.Commands;/namespace People.Api.IntegrationTests.Commands;/' \
    -e 's/^namespace People\.IntegrationTests\.Endpoints;/namespace People.Api.IntegrationTests.Endpoints;/' \
    -e 's/^namespace People\.IntegrationTests\.EventHandlers;/namespace People.Api.IntegrationTests.EventHandlers;/' \
    -e 's/^namespace People\.IntegrationTests\.Grpc;/namespace People.Api.IntegrationTests.Grpc;/' \
    -e 's/^namespace People\.IntegrationTests\.Infrastructure;/namespace People.Api.IntegrationTests.Infrastructure;/' \
    -e 's/^namespace People\.IntegrationTests\.Queries;/namespace People.Api.IntegrationTests.Queries;/' \
    -e 's/^namespace People\.IntegrationTests\.Web;/namespace People.Api.IntegrationTests.Web;/' \
    {} +
```

- [ ] **Step 7: Update using directives that reference the shared infrastructure**

```bash
find tests/Integration/People.Api.IntegrationTests -name "*.cs" -exec \
  sed -i '' \
    's/using People\.IntegrationTests\.Infrastructure;/using People.IntegrationTests.Shared.Infrastructure;/g' \
    {} +
```

- [ ] **Step 8: Update using directives that reference other old namespaces**

```bash
find tests/Integration/People.Api.IntegrationTests -name "*.cs" -exec \
  sed -i '' \
    -e 's/using People\.IntegrationTests\.Commands;/using People.Api.IntegrationTests.Commands;/g' \
    -e 's/using People\.IntegrationTests\.Queries;/using People.Api.IntegrationTests.Queries;/g' \
    -e 's/using People\.IntegrationTests\.Web;/using People.Api.IntegrationTests.Web;/g' \
    {} +
```

- [ ] **Step 9: Build API test project to verify it compiles**

```bash
dotnet build tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj --verbosity minimal
```

Expected: `Build succeeded.`

- [ ] **Step 10: Commit**

```bash
git add tests/Integration/People.Api.IntegrationTests/
git commit -m "Add People.Api.IntegrationTests project with API and application tests"
```

---

### Task 4: Update solution file and remove old project

**Files:**
- Modify: `Elwark.People.slnx`
- Delete: `tests/Integration/People.IntegrationTests/` (entire directory)

- [ ] **Step 1: Replace the `/tests/` folder in Elwark.People.slnx**

Open `Elwark.People.slnx` and replace the entire `<Folder Name="/tests/">` block with:

```xml
    <Folder Name="/tests/">
        <Project Path="tests/Unit/People.UnitTests/People.UnitTests.csproj"/>
        <Project Path="tests/Integration/People.IntegrationTests.Shared/People.IntegrationTests.Shared.csproj"/>
        <Project Path="tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj"/>
        <Project Path="tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj"/>
    </Folder>
```

- [ ] **Step 2: Build the full solution to verify all projects resolve**

```bash
dotnet build Elwark.People.slnx --configuration Release --verbosity minimal
```

Expected: `Build succeeded.`

- [ ] **Step 3: Delete the old integration test project directory**

```bash
rm -rf tests/Integration/People.IntegrationTests/
```

- [ ] **Step 4: Build again to confirm nothing references the deleted directory**

```bash
dotnet build Elwark.People.slnx --configuration Release --verbosity minimal
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add Elwark.People.slnx
git add tests/Integration/People.IntegrationTests/  # stages the deletion
git commit -m "Update solution: replace old IntegrationTests project with three new projects"
```

---

### Task 5: Update GitHub Actions workflow

**Files:**
- Modify: `.github/workflows/people.yml`

- [ ] **Step 1: Replace the integration-tests job with two parallel jobs**

In `.github/workflows/people.yml`, replace the entire `integration-tests:` job block with these two jobs:

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
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/Directory.Packages.props') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Build
        run: dotnet build Elwark.People.slnx --configuration Release --verbosity minimal

      - name: Test
        run: dotnet test tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj --configuration Release --no-build --no-restore --verbosity minimal --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults

      - name: Upload coverage
        uses: actions/upload-artifact@v6
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
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/Directory.Packages.props') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Build
        run: dotnet build Elwark.People.slnx --configuration Release --verbosity minimal

      - name: Test
        run: dotnet test tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj --configuration Release --no-build --no-restore --verbosity minimal --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults

      - name: Upload coverage
        uses: actions/upload-artifact@v6
        with:
          name: coverage-worker-integration
          path: TestResults/**/coverage.cobertura.xml
          if-no-files-found: error
          retention-days: 1
```

- [ ] **Step 2: Update coverage-report job's needs list**

Find the `coverage-report:` job and update the `needs` line:

```yaml
  coverage-report:
    name: Coverage Report
    runs-on: ubuntu-latest
    needs: [ unit-tests, api-integration-tests, worker-integration-tests ]
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/people.yml
git commit -m "Split integration-tests CI job into parallel api and worker jobs"
```

---

### Task 6: Final verification

- [ ] **Step 1: Build full solution in Release**

```bash
dotnet build Elwark.People.slnx --configuration Release --verbosity minimal
```

Expected: `Build succeeded.` with no warnings about missing projects.

- [ ] **Step 2: Run Worker integration tests locally**

```bash
dotnet test tests/Integration/People.Worker.IntegrationTests/People.Worker.IntegrationTests.csproj \
  --configuration Release --no-build --verbosity normal
```

Expected: All tests pass. Testcontainers starts a PostgreSQL container automatically.

- [ ] **Step 3: Run API integration tests locally**

```bash
dotnet test tests/Integration/People.Api.IntegrationTests/People.Api.IntegrationTests.csproj \
  --configuration Release --no-build --verbosity normal
```

Expected: All tests pass.

- [ ] **Step 4: Confirm old project directory is gone**

```bash
ls tests/Integration/
```

Expected output contains only:
```
People.Api.IntegrationTests
People.IntegrationTests.Shared
People.Worker.IntegrationTests
```
