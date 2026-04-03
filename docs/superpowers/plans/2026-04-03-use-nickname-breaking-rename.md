# UseNickname Breaking Rename Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename `PreferNickname` to `UseNickname` across domain, application, API, EF mapping, tests, and regenerated migrations.

**Architecture:** Apply a hard rename from `PreferNickname`/`preferNickname` to `UseNickname`/`useNickname` at every layer, keeping behavior unchanged. Update the EF owned mapping and regenerate the People migrations baseline with the repo script so the schema matches the new property and column names.

**Tech Stack:** C#, .NET, xUnit, ASP.NET Core minimal APIs, EF Core

---

### Task 1: Rename The Domain And Application Surface

**Files:**
- Modify: `src/People.Domain/ValueObjects/Name.cs`
- Modify: `src/People.Domain/Entities/Account.cs`
- Modify: `src/People.Application/Commands/UpdateAccount/UpdateAccountCommand.cs`
- Modify: `src/People.Application/Commands/EnrichAccount/EnrichAccountCommand.cs`
- Modify: `src/People.Application/Queries/GetAccountSummary/GetAccountSummaryQuery.cs` if needed by compile feedback
- Test: `tests/Unit/Unit.Api.Tests/Domain/ValueObjects/NameTests.cs`
- Test: `tests/Unit/Unit.Api.Tests/Domain/Entities/AccountTests.cs`
- Test: `tests/Unit/Unit.Api.Tests/Application/Commands/UpdateAccountCommandTests.cs`

- [ ] **Step 1: Write the failing test updates**

```csharp
Assert.True(name.UseNickname);
var name = Name.Create(Nickname.Parse("nick"), "John", "Doe", useNickname: true);
Assert.True(account.Name.UseNickname);
var request = new UpdateAccountCommand(
    account.Id,
    "Ada",
    "Lovelace",
    Nickname.Parse("ada"),
    UseNickname: false,
    Language.English,
    RegionCode.Empty,
    CountryCode.Empty,
    TimeZone.Utc,
    DateFormat.Default,
    TimeFormat.Default,
    DayOfWeek.Monday
);
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter "NameTests|AccountTests|UpdateAccountCommandTests"`
Expected: FAIL with compile errors because `UseNickname` does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
public bool UseNickname { get; }

private Name(Nickname nickname, string? firstName, string? lastName, bool useNickname)
{
    Nickname = nickname;
    FirstName = firstName;
    LastName = lastName;
    UseNickname = useNickname;
}

public static Name Create(
    Nickname nickname,
    string? firstName = null,
    string? lastName = null,
    bool useNickname = true
)
{
    ...
    return new Name(nickname, firstName, lastName, useNickname);
}
```

Also rename downstream property/parameter usage from `PreferNickname` to `UseNickname`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter "NameTests|AccountTests|UpdateAccountCommandTests"`
Expected: PASS

### Task 2: Rename API Contracts And Integration Expectations

**Files:**
- Modify: `src/People.Api/Endpoints/AccountEndpoints.cs`
- Modify: `src/People.Api/Messages/AccountReply.cs`
- Modify: `tests/Unit/Unit.Api.Tests/Infrastructure/Validators/UpdateRequestValidatorTests.cs`
- Modify: `tests/Integration/Integration.Api.Tests/Endpoints/AccountMeTests.cs`
- Modify: `tests/Integration/Integration.Api.Tests/Commands/UpdateAccountFlowTests.cs`
- Modify: `tests/Integration/Integration.Api.Tests/Queries/GetAccountSummaryQueryTests.cs`
- Modify: `tests/Integration/Integration.Api.Tests/Queries/GetAccountDetailsQueryTests.cs`
- Modify: `tests/Integration/Integration.Api.Tests/Grpc/PeopleServiceGetAccountTests.cs`

- [ ] **Step 1: Write the failing contract/test updates**

```csharp
var body = """
{
  "firstName": null,
  "lastName": null,
  "nickname": "after-put-nick",
  "useNickname": true,
  "language": "en",
  "countryCode": "US",
  "timeZone": "UTC",
  "dateFormat": "yyyy-MM-dd",
  "timeFormat": "HH:mm",
  "startOfWeek": "monday"
}
""";
```

```csharp
Assert.False(result.Name.UseNickname);
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter UpdateRequestValidatorTests`
Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "AccountMeTests|UpdateAccountFlowTests|GetAccountSummaryQueryTests|GetAccountDetailsQueryTests|PeopleServiceGetAccountTests"`
Expected: FAIL because contracts and assertions still reference `PreferNickname`.

- [ ] **Step 3: Write minimal implementation**

```csharp
public sealed record NameReply(
    Nickname Nickname,
    string? FirstName,
    string? LastName,
    bool UseNickname
);

public sealed record UpdateRequest(
    string? FirstName,
    string? LastName,
    string Nickname,
    bool UseNickname,
    ...
);
```

Also update endpoint mapping and JSON expectations to `useNickname`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter UpdateRequestValidatorTests`
Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "AccountMeTests|UpdateAccountFlowTests|GetAccountSummaryQueryTests|GetAccountDetailsQueryTests|PeopleServiceGetAccountTests"`
Expected: PASS

### Task 3: Rename EF Mapping And Regenerate Migrations

**Files:**
- Modify: `src/People.Infrastructure/EntityConfigurations/AccountEntityTypeConfiguration.cs`
- Delete/Recreate: `src/People.Infrastructure/Migrations/People`
- Modify: `tests/Integration/Integration.Api.Tests/Infrastructure/EntityConfigurationTests.cs`

- [ ] **Step 1: Write the failing EF test updates**

```csharp
Assert.False(account.Name.UseNickname);
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter EntityConfigurationTests`
Expected: FAIL because EF mapping/tests still reference `PreferNickname`.

- [ ] **Step 3: Write minimal implementation**

```csharp
navigationBuilder.Property(x => x.UseNickname)
    .HasColumnName("UseNickname");
```

Remove the existing People migrations folder contents and run:

```bash
./src/People.Infrastructure/add_migration.sh Init
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter EntityConfigurationTests`
Expected: PASS

### Task 4: Full Verification

**Files:**
- Verify all touched files

- [ ] **Step 1: Run full unit verification**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj`
Expected: PASS

- [ ] **Step 2: Run full integration verification**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj`
Expected: PASS

- [ ] **Step 3: Inspect git diff for migration regeneration and contract rename completeness**

Run: `git diff -- src/People.Domain src/People.Application src/People.Api src/People.Infrastructure tests`
Expected: Only intentional `UseNickname` rename and migration regeneration changes
