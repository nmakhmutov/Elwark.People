# Confirmation Event-Driven Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor email verification request to be event-driven — `Account` raises `EmailVerificationRequestedDomainEvent`, the outbox delivers it, and a background handler creates the `Confirmation` record and sends the email, guaranteeing consistency.

**Architecture:** `ConfirmingEmailCommandHandler` checks throttle, calls `account.RequestEmailVerification()` (which raises the domain event and returns a pre-generated confirmation ID), computes an encrypted token from that ID, and saves. The outbox pipeline captures the event and eventually delivers `SendEmailVerificationCommand` to a new handler that creates the `Confirmation` record and sends the email. The verify side (`ConfirmEmailCommand`) is unchanged.

**Tech Stack:** C# 14, .NET 10, EF Core (PeopleDbContext), Mediator (community), HybridCache, NSubstitute + xUnit for tests.

---

## File Map

| Action | File |
|--------|------|
| Create | `src/People.Domain/DomainEvents/EmailVerificationRequestedDomainEvent.cs` |
| Modify | `src/People.Domain/Entities/Account.cs` |
| Modify | `src/People.Application/Providers/Confirmation/IConfirmationService.cs` |
| Modify | `src/People.Infrastructure/Confirmations/Confirmation.cs` |
| Modify | `src/People.Infrastructure/Confirmations/ConfirmationService.cs` |
| Create | `src/People.Domain/IntegrationEvents/EmailVerificationRequestedIntegrationEvent.cs` |
| Create | `src/People.Infrastructure/Mappers/EmailVerificationRequestedMapper.cs` |
| Create | `src/People.Application/Commands/SendEmailVerification/SendEmailVerificationCommand.cs` |
| Modify | `src/People.Application/Commands/ConfirmingEmail/ConfirmingEmailCommand.cs` |
| Modify | `src/People.Infrastructure/HostingExtensions.cs` |
| Modify | `src/People.Worker/Jobs/OutboxDispatchJob.cs` |
| Create | `tests/Unit/People.UnitTests/Domain/Entities/AccountRequestEmailVerificationTests.cs` |
| Create | `tests/Unit/People.UnitTests/Application/Commands/SendEmailVerificationCommandTests.cs` |
| Modify | `tests/Unit/People.UnitTests/Application/Commands/ConfirmingEmailCommandTests.cs` |

---

### Task 1: Add `EmailVerificationRequestedDomainEvent` and `Account.RequestEmailVerification`

**Files:**
- Create: `src/People.Domain/DomainEvents/EmailVerificationRequestedDomainEvent.cs`
- Modify: `src/People.Domain/Entities/Account.cs`
- Create: `tests/Unit/People.UnitTests/Domain/Entities/AccountRequestEmailVerificationTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Unit/People.UnitTests/Domain/Entities/AccountRequestEmailVerificationTests.cs`:

```csharp
using System.Net.Mail;
using NSubstitute;
using People.Domain.Entities;
using People.Domain.DomainEvents;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Domain.Entities;

public sealed class AccountRequestEmailVerificationTests
{
    private static readonly AccountId AccountId = new(500L);
    private static readonly DateTime Utc = new(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc);

    private static TimeProvider FixedTime()
    {
        var tp = Substitute.For<TimeProvider>();
        tp.GetUtcNow().Returns(new DateTimeOffset(Utc, TimeSpan.Zero));
        return tp;
    }

    private static Account MakeAccountWithUnconfirmedEmail(string primary = "p@test.com", string unconfirmed = "u@test.com")
    {
        var time = FixedTime();
        var account = Application.Commands.EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(
            AccountId, time, primary: primary, pending: unconfirmed
        );
        return account;
    }

    [Fact]
    public void RequestEmailVerification_ValidUnconfirmedEmail_RaisesEvent()
    {
        var account = MakeAccountWithUnconfirmedEmail(unconfirmed: "u@test.com");
        var time = FixedTime();

        var confirmationId = account.RequestEmailVerification(new MailAddress("u@test.com"), time);

        Assert.NotEqual(Guid.Empty, confirmationId);
        var evt = account.GetDomainEvents()
            .OfType<EmailVerificationRequestedDomainEvent>()
            .Single();
        Assert.Equal(AccountId, evt.Id);
        Assert.Equal(confirmationId, evt.ConfirmationId);
        Assert.Equal("u@test.com", evt.Email.Address);
        Assert.Equal(Utc, evt.OccurredAt);
    }

    [Fact]
    public void RequestEmailVerification_EmailNotOnAccount_ThrowsEmailException()
    {
        var account = MakeAccountWithUnconfirmedEmail();
        var time = FixedTime();

        Assert.Throws<EmailException>(() =>
            account.RequestEmailVerification(new MailAddress("notfound@test.com"), time));
    }

    [Fact]
    public void RequestEmailVerification_AlreadyConfirmedEmail_ThrowsEmailException()
    {
        var time = FixedTime();
        var account = Application.Commands.EmailHandlerTestAccounts.AccountWithConfirmedPrimary(
            AccountId, time, primaryEmail: "confirmed@test.com"
        );

        Assert.Throws<EmailException>(() =>
            account.RequestEmailVerification(new MailAddress("confirmed@test.com"), time));
    }

    [Fact]
    public void RequestEmailVerification_ReturnedGuid_MatchesEventConfirmationId()
    {
        var account = MakeAccountWithUnconfirmedEmail(unconfirmed: "match@test.com");
        var time = FixedTime();

        var confirmationId = account.RequestEmailVerification(new MailAddress("match@test.com"), time);

        var evt = account.GetDomainEvents()
            .OfType<EmailVerificationRequestedDomainEvent>()
            .Single();
        Assert.Equal(confirmationId, evt.ConfirmationId);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail to compile**

```bash
cd /Users/nailmakhmutov/Downloads/Elwark.People
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj --no-build -v q 2>&1 | head -30
```

Expected: build failure — `EmailVerificationRequestedDomainEvent` and `RequestEmailVerification` do not exist yet.

- [ ] **Step 3: Create `EmailVerificationRequestedDomainEvent`**

Create `src/People.Domain/DomainEvents/EmailVerificationRequestedDomainEvent.cs`:

```csharp
using System.Net.Mail;
using People.Domain.Events;
using People.Domain.ValueObjects;

namespace People.Domain.DomainEvents;

public sealed record EmailVerificationRequestedDomainEvent(
    AccountId Id,
    Guid ConfirmationId,
    MailAddress Email,
    Language Language,
    DateTime OccurredAt
) : IDomainEvent;
```

- [ ] **Step 4: Add `RequestEmailVerification` to `Account`**

In `src/People.Domain/Entities/Account.cs`, add this method after `ConfirmEmail`:

```csharp
public Guid RequestEmailVerification(MailAddress email, TimeProvider timeProvider)
{
    var emailAccount = _emails.FirstOrDefault(x => x.Email == email.Address)
        ?? throw EmailException.NotFound(email);

    if (emailAccount.IsConfirmed)
        throw EmailException.AlreadyConfirmed(email);

    var confirmationId = Guid.CreateVersion7();
    AddDomainEvent(new EmailVerificationRequestedDomainEvent(Id, confirmationId, email, Language, timeProvider.UtcNow()));

    return confirmationId;
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q --filter "FullyQualifiedName~AccountRequestEmailVerificationTests"
```

Expected: 4 tests pass.

- [ ] **Step 6: Run full unit tests and commit**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass.

```bash
git add src/People.Domain/DomainEvents/EmailVerificationRequestedDomainEvent.cs \
        src/People.Domain/Entities/Account.cs \
        tests/Unit/People.UnitTests/Domain/Entities/AccountRequestEmailVerificationTests.cs
```

---

### Task 2: Update `IConfirmationService`, `ConfirmationService`, and `Confirmation`

**Files:**
- Modify: `src/People.Application/Providers/Confirmation/IConfirmationService.cs`
- Modify: `src/People.Infrastructure/Confirmations/Confirmation.cs`
- Modify: `src/People.Infrastructure/Confirmations/ConfirmationService.cs`

- [ ] **Step 1: Update `IConfirmationService`**

Replace the full file `src/People.Application/Providers/Confirmation/IConfirmationService.cs`:

```csharp
using System.Net.Mail;
using People.Domain.Entities;

namespace People.Application.Providers.Confirmation;

public interface IConfirmationService
{
    Task<ConfirmationResult> SignInAsync(AccountId id, CancellationToken ct = default);

    Task<AccountId> SignInAsync(string token, string code, CancellationToken ct = default);

    Task<ConfirmationResult> SignUpAsync(AccountId id, CancellationToken ct = default);

    Task<AccountId> SignUpAsync(string token, string code, CancellationToken ct = default);

    /// <summary>Checks the 1-minute resend throttle. Throws ConfirmationException.AlreadySent if within the window.</summary>
    Task ThrottleEmailVerificationAsync(AccountId id, CancellationToken ct = default);

    /// <summary>Computes the AES-encrypted Base64 token from a pre-generated confirmation ID and email address.</summary>
    string CreateEmailVerificationToken(Guid confirmationId, MailAddress email);

    Task<EmailConfirmation> VerifyEmailAsync(string token, string code, CancellationToken ct = default);

    Task<int> DeleteAsync(AccountId id, CancellationToken ct = default);
}
```

Note: `VerifyEmailAsync(AccountId id, MailAddress email, ct)` (the creation overload) is intentionally removed.

- [ ] **Step 2: Add explicit-ID constructor to `Confirmation`**

In `src/People.Infrastructure/Confirmations/Confirmation.cs`, add a second public constructor:

```csharp
public Confirmation(Guid id, AccountId accountId, string code, string type, DateTime createdAt, TimeSpan ttl)
{
    Id = id;
    AccountId = accountId;
    Code = code;
    Type = type;
    CreatedAt = createdAt;
    ExpiresAt = createdAt + ttl;
}
```

Place it after the existing `public Confirmation(AccountId accountId, ...)` constructor.

- [ ] **Step 3: Update `ConfirmationService`**

Replace the full file `src/People.Infrastructure/Confirmations/ConfirmationService.cs`:

```csharp
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using People.Application.Providers.Confirmation;
using People.Domain;
using People.Domain.Entities;

namespace People.Infrastructure.Confirmations;

internal sealed class ConfirmationService : IConfirmationService
{
    private const int ConfirmationLength = 6;

    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(1);

    private readonly HybridCache _cache;
    private readonly PeopleDbContext _dbContext;
    private readonly AppSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    public ConfirmationService(
        PeopleDbContext dbContext,
        HybridCache cache,
        IOptions<AppSecurityOptions> options,
        TimeProvider timeProvider
    )
    {
        _cache = cache;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<ConfirmationResult> SignInAsync(AccountId id, CancellationToken ct)
    {
        var confirmation = await EncodeAsync(id, "SignIn", ct);

        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public Task<AccountId> SignInAsync(string token, string code, CancellationToken ct) =>
        DecodeAsync(ConventToGuid(token), "SignIn", code, ct);

    public async Task<ConfirmationResult> SignUpAsync(AccountId id, CancellationToken ct)
    {
        var confirmation = await EncodeAsync(id, "SignUp", ct);

        return new ConfirmationResult(Convert.ToBase64String(confirmation.Id.ToByteArray()), confirmation.Code);
    }

    public Task<AccountId> SignUpAsync(string token, string code, CancellationToken ct) =>
        DecodeAsync(ConventToGuid(token), "SignUp", code, ct);

    public async Task ThrottleEmailVerificationAsync(AccountId id, CancellationToken ct)
    {
        var now = _timeProvider.UtcNow();

        var ttl = await _cache.GetOrCreateAsync(
            $"ppl-conf-lk-EmailVerify-{id}",
            _ => ValueTask.FromResult(now),
            new HybridCacheEntryOptions
            {
                Expiration = LockTtl,
                LocalCacheExpiration = LockTtl,
            },
            null,
            ct
        );

        if (now != ttl)
            throw ConfirmationException.AlreadySent();
    }

    public string CreateEmailVerificationToken(Guid confirmationId, MailAddress email)
    {
        var bytes = Encrypt(
            confirmationId.ToByteArray().Concat(Encoding.UTF8.GetBytes(email.Address)).ToArray()
        );
        return Convert.ToBase64String(bytes);
    }

    public async Task<EmailConfirmation> VerifyEmailAsync(string token, string code, CancellationToken ct)
    {
        try
        {
            var bytes = Decrypt(Convert.FromBase64String(token));
            var id = new Guid(bytes[..16]);
            var email = new MailAddress(Encoding.UTF8.GetString(bytes[16..]));

            var accountId = await DecodeAsync(id, "EmailVerify", code, ct);

            return new EmailConfirmation(accountId, email);
        }
        catch (ConfirmationException)
        {
            throw;
        }
        catch
        {
            throw ConfirmationException.Mismatch();
        }
    }

    public Task<int> DeleteAsync(AccountId id, CancellationToken ct) =>
        _dbContext.Confirmations.Where(x => x.AccountId == id).ExecuteDeleteAsync(ct);

    private byte[] Encrypt(byte[] bytes)
    {
        using var aes = Aes.Create();
        aes.Key = _options.AppKey;
        aes.IV = _options.AppVector;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    private byte[] Decrypt(byte[] bytes)
    {
        using var aes = Aes.Create();
        aes.Key = _options.AppKey;
        aes.IV = _options.AppVector;

        using var encryptor = aes.CreateDecryptor();
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    private async Task<Confirmation> EncodeAsync(AccountId id, string type, CancellationToken ct)
    {
        var now = _timeProvider.UtcNow();

        var ttl = await _cache.GetOrCreateAsync(
            $"ppl-conf-lk-{type}-{id}",
            _ => ValueTask.FromResult(now),
            new HybridCacheEntryOptions
            {
                Expiration = LockTtl,
                LocalCacheExpiration = LockTtl,
            },
            null,
            ct
        );

        if (now != ttl)
            throw ConfirmationException.AlreadySent();

        var confirmation = await _dbContext.Confirmations
            .FirstOrDefaultAsync(x => x.AccountId == id && x.Type == type, ct);

        if (confirmation is not null)
            return confirmation;

        var code = CreateCode(ConfirmationLength);

        var entity = new Confirmation(id, code, type, now, CodeTtl);
        await _dbContext.Confirmations.AddAsync(entity, ct);

        await _dbContext.SaveChangesAsync(ct);

        return entity;
    }

    private async Task<AccountId> DecodeAsync(Guid id, string type, string code, CancellationToken ct)
    {
        var confirmation = await _dbContext.Confirmations
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ConfirmationException.NotFound();

        if (!string.Equals(confirmation.Type, type, StringComparison.OrdinalIgnoreCase))
            throw ConfirmationException.Mismatch();

        if (!string.Equals(confirmation.Code, code, StringComparison.OrdinalIgnoreCase))
            throw ConfirmationException.Mismatch();

        return confirmation.AccountId;
    }

    private static Guid ConventToGuid(string token)
    {
        try
        {
            return new Guid(Convert.FromBase64String(token));
        }
        catch
        {
            throw ConfirmationException.Mismatch();
        }
    }

    private static string CreateCode(int length)
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

        return RandomNumberGenerator.GetString(chars, length);
    }
}
```

- [ ] **Step 4: Build to confirm compilation**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj -v q
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 5: Run full unit tests and commit**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass.

```bash
git add src/People.Application/Providers/Confirmation/IConfirmationService.cs \
        src/People.Infrastructure/Confirmations/Confirmation.cs \
        src/People.Infrastructure/Confirmations/ConfirmationService.cs
```

---

### Task 3: Add `EmailVerificationRequestedIntegrationEvent` and mapper

**Files:**
- Create: `src/People.Domain/IntegrationEvents/EmailVerificationRequestedIntegrationEvent.cs`
- Create: `src/People.Infrastructure/Mappers/EmailVerificationRequestedMapper.cs`

- [ ] **Step 1: Create the integration event**

Create `src/People.Domain/IntegrationEvents/EmailVerificationRequestedIntegrationEvent.cs`:

```csharp
using People.Domain.Events;

namespace People.Domain.IntegrationEvents;

public sealed record EmailVerificationRequestedIntegrationEvent(
    Guid Id,
    long AccountId,
    Guid ConfirmationId,
    string Email,
    string Language,
    DateTime OccurredAt
) : IIntegrationEvent;
```

- [ ] **Step 2: Create the mapper**

Create `src/People.Infrastructure/Mappers/EmailVerificationRequestedMapper.cs`:

```csharp
using People.Domain.DomainEvents;
using People.Domain.IntegrationEvents;
using People.Infrastructure.Outbox;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Mappers;

public sealed class EmailVerificationRequestedMapper : IOutboxEventMapper<EmailVerificationRequestedDomainEvent>
{
    public OutboxMessage Map(EmailVerificationRequestedDomainEvent evt)
    {
        var payload = new EmailVerificationRequestedIntegrationEvent(
            Guid.CreateVersion7(),
            evt.Id,
            evt.ConfirmationId,
            evt.Email.Address,
            evt.Language.ToString(),
            evt.OccurredAt
        );

        return OutboxMessage.Create(payload);
    }
}
```

- [ ] **Step 3: Build to confirm compilation**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj -v q
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 4: Run full unit tests and commit**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass.

```bash
git add src/People.Domain/IntegrationEvents/EmailVerificationRequestedIntegrationEvent.cs \
        src/People.Infrastructure/Mappers/EmailVerificationRequestedMapper.cs
```

---

### Task 4: Add `SendEmailVerificationCommand` and handler with tests

**Files:**
- Create: `src/People.Application/Commands/SendEmailVerification/SendEmailVerificationCommand.cs`
- Create: `tests/Unit/People.UnitTests/Application/Commands/SendEmailVerificationCommandTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/Unit/People.UnitTests/Application/Commands/SendEmailVerificationCommandTests.cs`:

```csharp
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using People.Application.Commands.SendEmailVerification;
using People.Application.Providers;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Outbox;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class SendEmailVerificationCommandTests
{
    private static readonly AccountId AccountId = new(600L);
    private static readonly Guid ConfirmationId = Guid.Parse("11111111-1111-7111-8111-111111111111");
    private static readonly DateTime OccurredAt = new(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc);

    private static PeopleDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PeopleDbContext(options, OutboxPipeline<PeopleDbContext>.Empty, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_CreatesConfirmationRecord()
    {
        await using var db = CreateInMemoryDbContext();
        var notification = Substitute.For<INotificationSender>();

        var handler = new SendEmailVerificationCommandHandler(db, notification);
        var command = new SendEmailVerificationCommand(
            AccountId, ConfirmationId, "verify@test.com", "en", OccurredAt
        );

        await handler.Handle(command, CancellationToken.None);

        var saved = await db.Confirmations.FindAsync(ConfirmationId);
        Assert.NotNull(saved);
        Assert.Equal((AccountId)AccountId, saved.AccountId);
        Assert.Equal("EmailVerify", saved.Type);
        Assert.Equal(6, saved.Code.Length);
        Assert.Equal(OccurredAt + TimeSpan.FromMinutes(30), saved.ExpiresAt);
    }

    [Fact]
    public async Task Handle_SendsConfirmationEmail()
    {
        await using var db = CreateInMemoryDbContext();
        var notification = Substitute.For<INotificationSender>();

        var handler = new SendEmailVerificationCommandHandler(db, notification);
        var command = new SendEmailVerificationCommand(
            AccountId, ConfirmationId, "verify@test.com", "en", OccurredAt
        );

        await handler.Handle(command, CancellationToken.None);

        await notification.Received(1).SendConfirmationAsync(
            Arg.Is<MailAddress>(m => m.Address == "verify@test.com"),
            Arg.Is<string>(c => c.Length == 6),
            Arg.Is<Language>(l => l.ToString() == "en"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Handle_UsesProvidedConfirmationId()
    {
        await using var db = CreateInMemoryDbContext();
        var notification = Substitute.For<INotificationSender>();

        var handler = new SendEmailVerificationCommandHandler(db, notification);
        var command = new SendEmailVerificationCommand(
            AccountId, ConfirmationId, "verify@test.com", "en", OccurredAt
        );

        await handler.Handle(command, CancellationToken.None);

        var saved = await db.Confirmations.FindAsync(ConfirmationId);
        Assert.NotNull(saved);
        Assert.Equal(ConfirmationId, saved.Id);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail to compile**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj --no-build -v q 2>&1 | head -20
```

Expected: build failure — `SendEmailVerificationCommand` does not exist yet.

- [ ] **Step 3: Create `SendEmailVerificationCommand` and handler**

Create `src/People.Application/Commands/SendEmailVerification/SendEmailVerificationCommand.cs`:

```csharp
using System.Net.Mail;
using System.Security.Cryptography;
using Mediator;
using People.Application.Providers;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Infrastructure.Confirmations;

namespace People.Application.Commands.SendEmailVerification;

public sealed record SendEmailVerificationCommand(
    long AccountId,
    Guid ConfirmationId,
    string Email,
    string Language,
    DateTime OccurredAt
) : ICommand;

public sealed class SendEmailVerificationCommandHandler : ICommandHandler<SendEmailVerificationCommand>
{
    private const string ConfirmationType = "EmailVerify";
    private const string CodeChars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(30);

    private readonly PeopleDbContext _dbContext;
    private readonly INotificationSender _notification;

    public SendEmailVerificationCommandHandler(PeopleDbContext dbContext, INotificationSender notification)
    {
        _dbContext = dbContext;
        _notification = notification;
    }

    public async ValueTask<Unit> Handle(SendEmailVerificationCommand request, CancellationToken ct)
    {
        var code = RandomNumberGenerator.GetString(CodeChars, 6);
        var email = new MailAddress(request.Email);
        var accountId = new AccountId(request.AccountId);
        var language = Language.Parse(request.Language);

        var confirmation = new Confirmation(
            request.ConfirmationId,
            accountId,
            code,
            ConfirmationType,
            request.OccurredAt,
            CodeTtl
        );

        await _dbContext.Confirmations.AddAsync(confirmation, ct);
        await _notification.SendConfirmationAsync(email, code, language, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q --filter "FullyQualifiedName~SendEmailVerificationCommandTests"
```

Expected: 3 tests pass.

- [ ] **Step 5: Run full unit tests and commit**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass.

```bash
git add src/People.Application/Commands/SendEmailVerification/SendEmailVerificationCommand.cs \
        tests/Unit/People.UnitTests/Application/Commands/SendEmailVerificationCommandTests.cs
```

---

### Task 5: Refactor `ConfirmingEmailCommandHandler` and update tests

**Files:**
- Modify: `src/People.Application/Commands/ConfirmingEmail/ConfirmingEmailCommand.cs`
- Modify: `tests/Unit/People.UnitTests/Application/Commands/ConfirmingEmailCommandTests.cs`

- [ ] **Step 1: Replace `ConfirmingEmailCommandTests`**

Replace the full contents of `tests/Unit/People.UnitTests/Application/Commands/ConfirmingEmailCommandTests.cs`:

```csharp
using System.Net.Mail;
using NSubstitute;
using People.Application.Commands.ConfirmingEmail;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using Xunit;

namespace People.UnitTests.Application.Commands;

public sealed class ConfirmingEmailCommandTests
{
    private static readonly AccountId AccountId = new(200L);
    private static readonly DateTime Utc = new(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_PendingEmail_ReturnsToken()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(
            AccountId, time, pending: "pending@test.com");
        var repo = Substitute.For<IAccountRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
        repo.UnitOfWork.Returns(uow);
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .ThrottleEmailVerificationAsync(AccountId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        confirmation
            .CreateEmailVerificationToken(Arg.Any<Guid>(), Arg.Is<MailAddress>(m => m.Address == "pending@test.com"))
            .Returns("encrypted-token");

        var handler = new ConfirmingEmailCommandHandler(confirmation, repo, time);

        var result = await handler.Handle(
            new ConfirmingEmailCommand(AccountId, new MailAddress("pending@test.com")),
            CancellationToken.None);

        Assert.Equal("encrypted-token", result.Token);
        await confirmation.Received(1).ThrottleEmailVerificationAsync(AccountId, Arg.Any<CancellationToken>());
        confirmation.Received(1).CreateEmailVerificationToken(Arg.Any<Guid>(), Arg.Is<MailAddress>(m => m.Address == "pending@test.com"));
        await uow.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountMissing_ThrowsNotFound()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var handler = new ConfirmingEmailCommandHandler(
            Substitute.For<IConfirmationService>(),
            repo,
            time);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(
                new ConfirmingEmailCommand(AccountId, new MailAddress("a@test.com")),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrottleActive_ThrowsAlreadySent()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithUnconfirmedExtra(
            AccountId, time, pending: "pending@test.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var confirmation = Substitute.For<IConfirmationService>();
        confirmation
            .ThrottleEmailVerificationAsync(AccountId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(ConfirmationException.AlreadySent()));

        var handler = new ConfirmingEmailCommandHandler(confirmation, repo, time);

        await Assert.ThrowsAsync<ConfirmationException>(async () =>
            await handler.Handle(
                new ConfirmingEmailCommand(AccountId, new MailAddress("pending@test.com")),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedEmail_ThrowsEmailException()
    {
        var time = EmailHandlerTestAccounts.FixedTime(Utc);
        var account = EmailHandlerTestAccounts.AccountWithConfirmedPrimary(
            AccountId, time, primaryEmail: "confirmed@test.com");
        var repo = Substitute.For<IAccountRepository>();
        repo.GetAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);

        var handler = new ConfirmingEmailCommandHandler(
            Substitute.For<IConfirmationService>(),
            repo,
            time);

        await Assert.ThrowsAsync<EmailException>(async () =>
            await handler.Handle(
                new ConfirmingEmailCommand(AccountId, new MailAddress("confirmed@test.com")),
                CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail (handler not updated yet)**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q --filter "FullyQualifiedName~ConfirmingEmailCommandTests" 2>&1 | head -30
```

Expected: compile failure — `ConfirmingEmailCommandHandler` constructor does not match new signature yet.

- [ ] **Step 3: Replace `ConfirmingEmailCommandHandler`**

Replace the handler class in `src/People.Application/Commands/ConfirmingEmail/ConfirmingEmailCommand.cs`:

```csharp
using System.Net.Mail;
using Mediator;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.ConfirmingEmail;

public sealed record ConfirmingEmailCommand(AccountId Id, MailAddress Email) : ICommand<ConfirmingTokenModel>;

public sealed class ConfirmingEmailCommandHandler : ICommandHandler<ConfirmingEmailCommand, ConfirmingTokenModel>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmingEmailCommandHandler(
        IConfirmationService confirmation,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _confirmation = confirmation;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<ConfirmingTokenModel> Handle(ConfirmingEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct)
            ?? throw AccountException.NotFound(request.Id);

        var emailAccount = account.Emails
            .FirstOrDefault(x => x.Email == request.Email.Address)
            ?? throw EmailException.NotFound(request.Email);

        if (emailAccount.IsConfirmed)
            throw EmailException.AlreadyConfirmed(request.Email);

        await _confirmation.ThrottleEmailVerificationAsync(account.Id, ct);

        var confirmationId = account.RequestEmailVerification(request.Email, _timeProvider);
        var token = _confirmation.CreateEmailVerificationToken(confirmationId, request.Email);

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return new ConfirmingTokenModel(token);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q --filter "FullyQualifiedName~ConfirmingEmailCommandTests"
```

Expected: 4 tests pass.

- [ ] **Step 5: Run the full unit test suite to check for regressions**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass.

- [ ] **Step 6: Run full unit tests and commit**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass.

```bash
git add src/People.Application/Commands/ConfirmingEmail/ConfirmingEmailCommand.cs \
        tests/Unit/People.UnitTests/Application/Commands/ConfirmingEmailCommandTests.cs
```

---

### Task 6: Wire up mapper and dispatch routing

**Files:**
- Modify: `src/People.Infrastructure/HostingExtensions.cs`
- Modify: `src/People.Worker/Jobs/OutboxDispatchJob.cs`

- [ ] **Step 1: Register the new mapper in `HostingExtensions.cs`**

In `src/People.Infrastructure/HostingExtensions.cs`, update the `AddOutbox` call to include the new mapper:

```csharp
builder.Services
    .AddOutbox<PeopleDbContext>(outbox => outbox
        .AddMapper(new AccountCreatedMapper())
        .AddMapper(new AccountUpdatedMapper())
        .AddMapper(new AccountDeletedMapper())
        .AddMapper(new EmailVerificationRequestedMapper())
    );
```

- [ ] **Step 2: Add the new routing case to `OutboxDispatchJob.GetCommands`**

In `src/People.Worker/Jobs/OutboxDispatchJob.cs`, add the new using and update `GetCommands`:

Add at the top of usings:
```csharp
using People.Application.Commands.SendEmailVerification;
using People.Domain.IntegrationEvents;
```

Replace the `GetCommands` method:

```csharp
private static IEnumerable<ICommand> GetCommands(IIntegrationEvent payload) =>
    payload switch
    {
        AccountCreatedIntegrationEvent x =>
        [
            new EnrichAccountCommand(x.AccountId, x.IpAddress),
            new SendWebhooksCommand(x.AccountId, WebhookType.Created, x.OccurredAt)
        ],
        AccountUpdatedIntegrationEvent x =>
        [
            new SendWebhooksCommand(x.AccountId, WebhookType.Updated, x.OccurredAt)
        ],
        AccountDeletedIntegrationEvent x =>
        [
            new SendWebhooksCommand(x.AccountId, WebhookType.Deleted, x.OccurredAt)
        ],
        EmailVerificationRequestedIntegrationEvent x =>
        [
            new SendEmailVerificationCommand(x.AccountId, x.ConfirmationId, x.Email, x.Language, x.OccurredAt)
        ],
        _ => throw new ArgumentOutOfRangeException()
    };
```

- [ ] **Step 3: Build the full solution to confirm no compilation errors**

```bash
dotnet build -v q
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 4: Run the full unit test suite**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass, 0 failures.

- [ ] **Step 5: Run full unit tests and commit**

```bash
dotnet test tests/Unit/People.UnitTests/People.UnitTests.csproj -v q
```

Expected: all tests pass.

```bash
git add src/People.Infrastructure/HostingExtensions.cs \
        src/People.Worker/Jobs/OutboxDispatchJob.cs
```

---

## Done

All six tasks complete. The email verification request is now event-driven:

1. `ConfirmingEmailCommandHandler` checks throttle, calls `account.RequestEmailVerification`, gets back a confirmation ID, computes an encrypted token, saves — domain event is captured by outbox.
2. `OutboxDispatchJob` routes `EmailVerificationRequestedIntegrationEvent` → `SendEmailVerificationCommand`.
3. `SendEmailVerificationCommandHandler` creates the `Confirmation` record and sends the email atomically.

`ConfirmEmailCommandHandler` (the verify side) is untouched.
