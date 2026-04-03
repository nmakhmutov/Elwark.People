# Confirmation Service Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current overloaded confirmation service with explicit challenge and email-token services, enum-backed confirmation kinds, and lower-allocation verification paths.

**Architecture:** Keep the existing confirmation table and flow semantics, but split responsibilities into `IConfirmationChallengeService` for persistence/throttling/verification and `IEmailVerificationTokenService` for protected email-token payloads. Migrate callers from flow-specific overloads to explicit `IssueAsync` and `VerifyAsync` calls using `ConfirmationKind`, while preserving current exception semantics.

**Tech Stack:** C#, .NET, Entity Framework Core, HybridCache, xUnit, NSubstitute

---

### Task 1: Add application contracts and enum

**Files:**
- Create: `src/People.Application/Providers/Confirmation/ConfirmationKind.cs`
- Create: `src/People.Application/Providers/Confirmation/ConfirmationChallenge.cs`
- Create: `src/People.Application/Providers/Confirmation/EmailVerificationTokenPayload.cs`
- Create: `src/People.Application/Providers/Confirmation/IConfirmationChallengeService.cs`
- Create: `src/People.Application/Providers/Confirmation/IEmailVerificationTokenService.cs`
- Modify: `src/People.Application/Providers/Confirmation/IConfirmationService.cs`
- Test: `tests/Unit/Unit.Api.Tests/Application/Commands/SignInByEmailCommandTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task Handle_UsesChallengeServiceToVerifyEmailSignInChallenge()
{
    var confirmation = Substitute.For<IConfirmationChallengeService>();
    confirmation.VerifyAsync("token", "code", ConfirmationKind.EmailSignIn, Arg.Any<CancellationToken>())
        .Returns(new AccountId(1));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter Handle_UsesChallengeServiceToVerifyEmailSignInChallenge`
Expected: FAIL because `IConfirmationChallengeService` and `ConfirmationKind` do not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
public enum ConfirmationKind
{
    EmailSignIn = 1,
    EmailSignUp = 2,
    EmailConfirmation = 3,
}

public sealed record ConfirmationChallenge(Guid Id, string Token, string Code, ConfirmationKind Kind);

public sealed record EmailVerificationTokenPayload(Guid ConfirmationId, MailAddress Email);
```

```csharp
public interface IConfirmationChallengeService
{
    Task<ConfirmationChallenge> IssueAsync(AccountId accountId, ConfirmationKind kind, CancellationToken ct = default);
    Task<AccountId> VerifyAsync(string token, string code, ConfirmationKind kind, CancellationToken ct = default);
    Task<int> DeleteByAccountAsync(AccountId accountId, CancellationToken ct = default);
    Task ThrottleEmailVerificationAsync(AccountId accountId, CancellationToken ct = default);
}

public interface IEmailVerificationTokenService
{
    string CreateToken(Guid confirmationId, MailAddress email);
    EmailVerificationTokenPayload ParseToken(string token);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter Handle_UsesChallengeServiceToVerifyEmailSignInChallenge`
Expected: PASS or compile advances to the next missing caller updates.

- [ ] **Step 5: Commit**

```bash
git add src/People.Application/Providers/Confirmation tests/Unit/Unit.Api.Tests/Application/Commands/SignInByEmailCommandTests.cs
git commit -m "refactor: add confirmation challenge contracts"
```

### Task 2: Add failing infrastructure tests for challenge service and email token service

**Files:**
- Modify: `tests/Integration/Integration.Api.Tests/Infrastructure/ConfirmationServiceTests.cs`
- Create: `tests/Integration/Integration.Api.Tests/Infrastructure/EmailVerificationTokenServiceTests.cs`
- Test: `tests/Integration/Integration.Api.Tests/Infrastructure/ConfirmationServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public async Task IssueAsync_CreatesEmailSignUpChallenge_AndVerifyReturnsAccountId()
{
    var result = await sut.IssueAsync(accountId, ConfirmationKind.EmailSignUp, CancellationToken.None);
    var decoded = await sut.VerifyAsync(result.Token, result.Code, ConfirmationKind.EmailSignUp, CancellationToken.None);

    Assert.Equal(accountId, decoded);
    Assert.Equal(ConfirmationKind.EmailSignUp, result.Kind);
}

[Fact]
public void ParseToken_WithCreatedToken_ReturnsConfirmationIdAndEmail()
{
    var token = sut.CreateToken(confirmationId, email);
    var payload = sut.ParseToken(token);

    Assert.Equal(confirmationId, payload.ConfirmationId);
    Assert.Equal(email.Address, payload.Email.Address);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "IssueAsync_CreatesEmailSignUpChallenge_AndVerifyReturnsAccountId|ParseToken_WithCreatedToken_ReturnsConfirmationIdAndEmail"`
Expected: FAIL because the new APIs and service split do not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
internal sealed class EmailVerificationTokenService : IEmailVerificationTokenService
{
    public string CreateToken(Guid confirmationId, MailAddress email) => throw new NotImplementedException();
    public EmailVerificationTokenPayload ParseToken(string token) => throw new NotImplementedException();
}
```

```csharp
internal sealed class ConfirmationChallengeService : IConfirmationChallengeService
{
    public Task<ConfirmationChallenge> IssueAsync(AccountId accountId, ConfirmationKind kind, CancellationToken ct) => throw new NotImplementedException();
    public Task<AccountId> VerifyAsync(string token, string code, ConfirmationKind kind, CancellationToken ct) => throw new NotImplementedException();
    public Task<int> DeleteByAccountAsync(AccountId accountId, CancellationToken ct) => throw new NotImplementedException();
    public Task ThrottleEmailVerificationAsync(AccountId accountId, CancellationToken ct) => throw new NotImplementedException();
}
```

- [ ] **Step 4: Run tests to verify they still fail for behavior, not compilation**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "IssueAsync_CreatesEmailSignUpChallenge_AndVerifyReturnsAccountId|ParseToken_WithCreatedToken_ReturnsConfirmationIdAndEmail"`
Expected: FAIL with `NotImplementedException`.

- [ ] **Step 5: Commit**

```bash
git add tests/Integration/Integration.Api.Tests/Infrastructure src/People.Infrastructure/Confirmations
git commit -m "test: add confirmation refactor integration coverage"
```

### Task 3: Implement email verification token service

**Files:**
- Create: `src/People.Infrastructure/Confirmations/EmailVerificationTokenService.cs`
- Modify: `tests/Integration/Integration.Api.Tests/Infrastructure/EmailVerificationTokenServiceTests.cs`
- Test: `tests/Integration/Integration.Api.Tests/Infrastructure/EmailVerificationTokenServiceTests.cs`

- [ ] **Step 1: Write the failing behavior test**

```csharp
[Fact]
public void ParseToken_WithInvalidToken_ThrowsMismatch()
{
    var ex = Assert.Throws<ConfirmationException>(() => sut.ParseToken("not-base64"));
    Assert.Equal("Mismatch", ex.Code);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter ParseToken_WithInvalidToken_ThrowsMismatch`
Expected: FAIL because `ParseToken` throws `NotImplementedException`.

- [ ] **Step 3: Write minimal implementation**

```csharp
private static byte[] ComposePayload(Guid confirmationId, string email)
{
    var idBytes = confirmationId.ToByteArray();
    var emailBytes = Encoding.UTF8.GetBytes(email);
    var payload = new byte[idBytes.Length + emailBytes.Length];

    idBytes.CopyTo(payload, 0);
    emailBytes.CopyTo(payload, idBytes.Length);

    return payload;
}
```

```csharp
public EmailVerificationTokenPayload ParseToken(string token)
{
    try
    {
        var bytes = Decrypt(Convert.FromBase64String(token));
        return new EmailVerificationTokenPayload(new Guid(bytes[..16]), new MailAddress(Encoding.UTF8.GetString(bytes[16..])));
    }
    catch
    {
        throw ConfirmationException.Mismatch();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "ParseToken_WithCreatedToken_ReturnsConfirmationIdAndEmail|ParseToken_WithInvalidToken_ThrowsMismatch"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/People.Infrastructure/Confirmations/EmailVerificationTokenService.cs tests/Integration/Integration.Api.Tests/Infrastructure/EmailVerificationTokenServiceTests.cs
git commit -m "refactor: extract email verification token service"
```

### Task 4: Implement challenge service with enum mapping and lower-allocation paths

**Files:**
- Create: `src/People.Infrastructure/Confirmations/ConfirmationKindMap.cs`
- Modify: `src/People.Infrastructure/Confirmations/ConfirmationService.cs`
- Modify: `src/People.Infrastructure/Confirmations/Confirmation.cs`
- Modify: `src/People.Infrastructure/EntityConfigurations/ConfirmationEntityTypeConfiguration.cs`
- Test: `tests/Integration/Integration.Api.Tests/Infrastructure/ConfirmationServiceTests.cs`

- [ ] **Step 1: Write the failing behavior tests**

```csharp
[Fact]
public async Task VerifyAsync_WithWrongKind_ThrowsMismatch()
{
    var issued = await sut.IssueAsync(new AccountId(1), ConfirmationKind.EmailSignIn, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
        sut.VerifyAsync(issued.Token, issued.Code, ConfirmationKind.EmailSignUp, CancellationToken.None));

    Assert.Equal("Mismatch", ex.Code);
}

[Fact]
public async Task IssueAsync_SecondCallWithinLockWindow_ThrowsAlreadySent()
{
    _ = await sut.IssueAsync(new AccountId(2), ConfirmationKind.EmailSignUp, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<ConfirmationException>(() =>
        sut.IssueAsync(new AccountId(2), ConfirmationKind.EmailSignUp, CancellationToken.None));

    Assert.Equal("AlreadySent", ex.Code);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "VerifyAsync_WithWrongKind_ThrowsMismatch|IssueAsync_SecondCallWithinLockWindow_ThrowsAlreadySent"`
Expected: FAIL because `IssueAsync` and `VerifyAsync` are not implemented.

- [ ] **Step 3: Write minimal implementation**

```csharp
private static readonly HybridCacheEntryOptions LockCacheEntry = new()
{
    Expiration = LockTtl,
    LocalCacheExpiration = LockTtl,
};
```

```csharp
private static string ToStorageValue(ConfirmationKind kind) => kind switch
{
    ConfirmationKind.EmailSignIn => "EmailSignIn",
    ConfirmationKind.EmailSignUp => "EmailSignUp",
    ConfirmationKind.EmailConfirmation => "EmailConfirmation",
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
};
```

```csharp
var confirmation = await _dbContext.Confirmations
    .Where(x => x.Id == id)
    .Select(x => new { x.AccountId, x.Code, x.Type })
    .FirstOrDefaultAsync(ct) ?? throw ConfirmationException.NotFound();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "IssueAsync|VerifyAsync"`
Expected: PASS for challenge issuance and verification coverage.

- [ ] **Step 5: Commit**

```bash
git add src/People.Infrastructure/Confirmations src/People.Infrastructure/EntityConfigurations/ConfirmationEntityTypeConfiguration.cs tests/Integration/Integration.Api.Tests/Infrastructure/ConfirmationServiceTests.cs
git commit -m "refactor: implement confirmation challenge service"
```

### Task 5: Update application command handlers and unit tests

**Files:**
- Modify: `src/People.Application/Commands/SignInByEmail/SignInByEmailCommand.cs`
- Modify: `src/People.Application/Commands/SignUpByEmail/SignUpByEmailCommand.cs`
- Modify: `src/People.Application/Commands/ConfirmEmail/ConfirmEmailCommand.cs`
- Modify: `src/People.Application/Commands/SigningInByEmail/SigningInByEmailCommand.cs`
- Modify: `src/People.Application/Commands/SigningUpByEmail/SigningUpByEmailCommand.cs`
- Modify: `src/People.Application/Commands/ConfirmingEmail/ConfirmingEmailCommand.cs`
- Modify: `src/People.Application/Commands/EnrichAccount/EnrichAccountCommand.cs`
- Modify: `tests/Unit/Unit.Api.Tests/Application/Commands/*.cs`

- [ ] **Step 1: Write the failing unit tests**

```csharp
confirmation.Received(1)
    .VerifyAsync(request.Token, request.Code, ConfirmationKind.EmailSignIn, Arg.Any<CancellationToken>());

confirmation.Received(1)
    .IssueAsync(account.Id, ConfirmationKind.EmailSignUp, Arg.Any<CancellationToken>());
```

```csharp
tokenService.Received(1).CreateToken(Arg.Any<Guid>(), email);
tokenService.Received(1).ParseToken(request.Token);
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter "SignInByEmailCommandTests|SignUpByEmailCommandTests|ConfirmEmailCommandTests|ConfirmingEmailCommandTests|SigningInByEmailCommandTests|SigningUpByEmailCommandTests|EnrichAccountCommandTests"`
Expected: FAIL because the handlers still depend on `IConfirmationService`.

- [ ] **Step 3: Write minimal implementation**

```csharp
var id = await _confirmation.VerifyAsync(request.Token, request.Code, ConfirmationKind.EmailSignIn, ct);
```

```csharp
var challenge = await _confirmation.IssueAsync(account.Id, ConfirmationKind.EmailConfirmation, ct);
var token = _emailVerificationTokens.CreateToken(challenge.Id, email);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj --filter "FullyQualifiedName~Application.Commands"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/People.Application/Commands tests/Unit/Unit.Api.Tests/Application/Commands
git commit -m "refactor: update email confirmation command handlers"
```

### Task 6: Wire dependency injection and persistence migration

**Files:**
- Modify: `src/People.Infrastructure/HostingExtensions.cs`
- Modify: `tests/Integration/Integration.Api.Tests/Commands/CommandTestFixture.cs`
- Modify: `tests/Integration/Integration.Api.Tests/EventHandlers/IntegrationEventHandlerTestFixture.cs`
- Modify: `src/People.Infrastructure/Migrations/People/*.cs`
- Test: `tests/Integration/Integration.Api.Tests/Infrastructure/EntityConfigurationTests.cs`

- [ ] **Step 1: Write the failing integration test**

```csharp
[Fact]
public void ServiceProvider_ResolvesConfirmationChallengeAndEmailTokenServices()
{
    using var scope = provider.CreateScope();
    Assert.NotNull(scope.ServiceProvider.GetRequiredService<IConfirmationChallengeService>());
    Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEmailVerificationTokenService>());
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter ServiceProvider_ResolvesConfirmationChallengeAndEmailTokenServices`
Expected: FAIL because DI still registers `IConfirmationService`.

- [ ] **Step 3: Write minimal implementation**

```csharp
services
    .AddScoped<IConfirmationChallengeService, ConfirmationChallengeService>()
    .AddScoped<IEmailVerificationTokenService, EmailVerificationTokenService>();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "ServiceProvider_ResolvesConfirmationChallengeAndEmailTokenServices|EntityConfigurationTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/People.Infrastructure/HostingExtensions.cs src/People.Infrastructure/Migrations tests/Integration/Integration.Api.Tests
git commit -m "refactor: wire confirmation services and persistence updates"
```

### Task 7: Final verification and cleanup

**Files:**
- Modify: `src/People.Application/Providers/Confirmation/IConfirmationService.cs`
- Modify: `src/People.Infrastructure/Confirmations/ConfirmationService.cs`
- Test: `tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj`
- Test: `tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj`

- [ ] **Step 1: Remove obsolete compatibility shims**

```csharp
// Delete the old IConfirmationService interface and the old ConfirmationService type name
// after all callers, registrations, and tests use the new contracts.
```

- [ ] **Step 2: Run focused test suites**

Run: `dotnet test tests/Unit/Unit.Api.Tests/Unit.Api.Tests.csproj`
Expected: PASS.

Run: `dotnet test tests/Integration/Integration.Api.Tests/Integration.Api.Tests.csproj --filter "FullyQualifiedName~Confirmation|FullyQualifiedName~Email"`
Expected: PASS.

- [ ] **Step 3: Run broader regression coverage**

Run: `dotnet test`
Expected: PASS or a documented list of unrelated pre-existing failures.

- [ ] **Step 4: Inspect git diff**

Run: `git status --short`
Expected: only intended confirmation refactor changes remain unstaged or staged.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "refactor: split confirmation challenge and email token services"
```
