# Webhook DbContext Separation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract webhooks into a dedicated `WebhookDbContext` with its own migrations, a delivery queue (`WebhookMessage`), a consumer repository, and admin CRUD endpoints.

**Architecture:** `WebhookConsumer` (renamed from `Webhook`) and `WebhookMessage` are owned by a new `WebhookDbContext` that lives in the same Postgres database. The outbox job writes `WebhookMessage` rows instead of sending HTTP directly; a new `WebhookDispatchJob` fans out delivery with Polly-backed retry and per-message failure tracking.

**Tech Stack:** EF Core 10, Npgsql, Quartz.NET, Polly (via `AddStandardResilienceHandler`), Mediator, NSubstitute (tests), Testcontainers (tests)

---

## File Map

**Create:**
- `src/People.Application/Providers/Webhooks/WebhookConsumer.cs`
- `src/People.Application/Providers/Webhooks/WebhookMessageStatus.cs`
- `src/People.Application/Providers/Webhooks/WebhookMessage.cs`
- `src/People.Application/Providers/Webhooks/IWebhookConsumerRepository.cs`
- `src/People.Application/Commands/CreateWebhookMessage/CreateWebhookMessageCommand.cs`
- `src/People.Infrastructure/Webhooks/WebhookDbContext.cs`
- `src/People.Infrastructure/Webhooks/WebhookConsumerRepository.cs`
- `src/People.Infrastructure/Webhooks/CreateWebhookMessageCommandHandler.cs`
- `src/People.Infrastructure/EntityConfigurations/WebhookConsumerEntityTypeConfiguration.cs`
- `src/People.Infrastructure/EntityConfigurations/WebhookMessageEntityTypeConfiguration.cs`
- `src/People.Infrastructure/Migrations/People/` (move existing files here)
- `src/People.Infrastructure/Migrations/Webhooks/` (new migrations)
- `src/People.Worker/Jobs/WebhookDispatchJob.cs`
- `src/People.Api/Endpoints/WebhookEndpoints.cs`
- `tests/Integration/People.IntegrationTests/Commands/CreateWebhookMessageCommandTests.cs`
- `tests/Integration/People.IntegrationTests/Jobs/WebhookDispatchJobTests.cs`

**Modify:**
- `src/People.Application/Providers/Webhooks/IWebhookSender.cs` — update `Webhook` → `WebhookConsumer`
- `src/People.Infrastructure/PeopleDbContext.cs` — remove `Webhook` DbSet and configuration
- `src/People.Infrastructure/Webhooks/WebhookSender.cs` — update type + add `EnsureSuccessStatusCode`
- `src/People.Infrastructure/HostingExtensions.cs` — register `WebhookDbContext`, repository; remove old webhook services
- `src/People.Infrastructure/Migrations/PeopleDbContextModelSnapshot.cs` — remove `Webhook` entity block
- `src/People.Worker/Program.cs` — register `WebhookDispatchJob`
- `src/People.Worker/Jobs/OutboxDispatchJob.cs` — replace `SendWebhooksCommand` → `CreateWebhookMessageCommand`
- `src/People.Api/Program.cs` — map `WebhookEndpoints`
- `src/People.Api/add_migration.sh` — auto-detect context changes
- `tests/Integration/People.IntegrationTests/Infrastructure/PostgreSqlFixture.cs` — add `WebhookDbContext` migration
- `tests/Integration/People.IntegrationTests/Commands/CommandTestFixture.cs` — add `WebhookDbContext` to DI

**Delete:**
- `src/People.Application/Providers/Webhooks/Webhook.cs`
- `src/People.Application/Providers/Webhooks/IWebhookRetriever.cs`
- `src/People.Application/Commands/SendWebhooks/SendWebhooksCommand.cs`
- `src/People.Infrastructure/Webhooks/WebhookRetriever.cs`
- `src/People.Infrastructure/EntityConfigurations/WebhookSubscriptionEntityTypeConfiguration.cs`
- `tests/Integration/People.IntegrationTests/Commands/SendWebhooksCommandTests.cs`

---

### Task 1: Add `WebhookConsumer` entity

**Files:**
- Create: `src/People.Application/Providers/Webhooks/WebhookConsumer.cs`

- [ ] **Step 1: Create `WebhookConsumer.cs`**

```csharp
using JetBrains.Annotations;

namespace People.Application.Providers.Webhooks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class WebhookConsumer
{
    public int Id { get; private set; }
    public WebhookType Type { get; private set; }
    public WebhookMethod Method { get; private set; }
    public string DestinationUrl { get; private set; }
    public string? Token { get; private set; }

    public WebhookConsumer(WebhookType type, WebhookMethod method, string destinationUrl, string? token)
        : this(0, type, method, destinationUrl, token)
    {
    }

    private WebhookConsumer(int id, WebhookType type, WebhookMethod method, string destinationUrl, string? token)
    {
        Id = id;
        Type = type;
        Method = method;
        DestinationUrl = destinationUrl;
        Token = token;
    }

    public void Update(WebhookType type, WebhookMethod method, string destinationUrl, string? token)
    {
        Type = type;
        Method = method;
        DestinationUrl = destinationUrl;
        Token = token;
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build src/People.Application/People.Application.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 2: Add `WebhookMessageStatus` and `WebhookMessage`

**Files:**
- Create: `src/People.Application/Providers/Webhooks/WebhookMessageStatus.cs`
- Create: `src/People.Application/Providers/Webhooks/WebhookMessage.cs`

- [ ] **Step 1: Create `WebhookMessageStatus.cs`**

```csharp
namespace People.Application.Providers.Webhooks;

public enum WebhookMessageStatus : byte
{
    Pending = 0,
    Failed = 1
}
```

- [ ] **Step 2: Create `WebhookMessage.cs`**

```csharp
using JetBrains.Annotations;

namespace People.Application.Providers.Webhooks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class WebhookMessage
{
    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public WebhookType Type { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? RetryAfter { get; private set; }
    public WebhookMessageStatus Status { get; private set; }

    public WebhookMessage(long accountId, WebhookType type, DateTime occurredAt)
        : this(0, accountId, type, occurredAt, 0, null, WebhookMessageStatus.Pending)
    {
    }

    private WebhookMessage(
        long id, long accountId, WebhookType type, DateTime occurredAt,
        int attempts, DateTime? retryAfter, WebhookMessageStatus status)
    {
        Id = id;
        AccountId = accountId;
        Type = type;
        OccurredAt = occurredAt;
        Attempts = attempts;
        RetryAfter = retryAfter;
        Status = status;
    }

    public void MarkFailed(DateTime retryAfter)
    {
        Attempts++;
        if (Attempts >= 10)
            Status = WebhookMessageStatus.Failed;
        else
            RetryAfter = retryAfter;
    }
}
```

- [ ] **Step 3: Verify compile**

```bash
dotnet build src/People.Application/People.Application.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 3: Add application contracts

**Files:**
- Create: `src/People.Application/Providers/Webhooks/IWebhookConsumerRepository.cs`
- Modify: `src/People.Application/Providers/Webhooks/IWebhookSender.cs`
- Create: `src/People.Application/Commands/CreateWebhookMessage/CreateWebhookMessageCommand.cs`

- [ ] **Step 1: Create `IWebhookConsumerRepository.cs`**

```csharp
namespace People.Application.Providers.Webhooks;

public interface IWebhookConsumerRepository
{
    Task<WebhookConsumer?> GetAsync(int id, CancellationToken ct);
    IAsyncEnumerable<WebhookConsumer> GetAllAsync(CancellationToken ct);
    Task<WebhookConsumer> CreateAsync(WebhookConsumer consumer, CancellationToken ct);
    Task<WebhookConsumer> UpdateAsync(WebhookConsumer consumer, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
```

- [ ] **Step 2: Update `IWebhookSender.cs` — replace `Webhook` with `WebhookConsumer`**

Full file after edit:
```csharp
namespace People.Application.Providers.Webhooks;

public interface IWebhookSender
{
    Task SendAsync(long accountId, DateTime occurredAt, IEnumerable<WebhookConsumer> subscriptions, CancellationToken ct);
}
```

- [ ] **Step 3: Create `CreateWebhookMessageCommand.cs`**

The command record lives in Application. The handler lives in Infrastructure (Task 6) because it needs `WebhookDbContext` — Application must not reference Infrastructure types.

```csharp
using Mediator;
using People.Application.Providers.Webhooks;

namespace People.Application.Commands.CreateWebhookMessage;

public sealed record CreateWebhookMessageCommand(long AccountId, WebhookType Type, DateTime OccurredAt) : ICommand;
```

- [ ] **Step 4: Verify compile of Application project**

```bash
dotnet build src/People.Application/People.Application.csproj 2>&1 | head -20
```

Expected: Build succeeded.

- [ ] **Step 5: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 4: Create entity type configurations

**Files:**
- Create: `src/People.Infrastructure/EntityConfigurations/WebhookConsumerEntityTypeConfiguration.cs`
- Create: `src/People.Infrastructure/EntityConfigurations/WebhookMessageEntityTypeConfiguration.cs`

- [ ] **Step 1: Create `WebhookConsumerEntityTypeConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class WebhookConsumerEntityTypeConfiguration : IEntityTypeConfiguration<WebhookConsumer>
{
    public void Configure(EntityTypeBuilder<WebhookConsumer> builder)
    {
        builder.ToTable("webhooks");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Type)
            .HasDatabaseName("IX_webhook_subscriptions_type");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.Method)
            .HasColumnName("method")
            .IsRequired();

        builder.Property(x => x.DestinationUrl)
            .HasColumnName("destination_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Token)
            .HasColumnName("token")
            .HasMaxLength(256);
    }
}
```

- [ ] **Step 2: Create `WebhookMessageEntityTypeConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class WebhookMessageEntityTypeConfiguration : IEntityTypeConfiguration<WebhookMessage>
{
    public void Configure(EntityTypeBuilder<WebhookMessage> builder)
    {
        builder.ToTable("webhook_messages");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Status, x.RetryAfter })
            .HasDatabaseName("IX_webhook_messages_status_retry_after");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(x => x.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.RetryAfter)
            .HasColumnName("retry_after");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();
    }
}
```

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 5: Create `WebhookDbContext`

**Files:**
- Create: `src/People.Infrastructure/Webhooks/WebhookDbContext.cs`

- [ ] **Step 1: Create `WebhookDbContext.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using People.Application.Providers.Webhooks;
using People.Infrastructure.EntityConfigurations;

namespace People.Infrastructure.Webhooks;

public sealed class WebhookDbContext : DbContext
{
    public DbSet<WebhookConsumer> WebhookConsumers => Set<WebhookConsumer>();
    public DbSet<WebhookMessage> WebhookMessages => Set<WebhookMessage>();

    public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WebhookConsumerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookMessageEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class WebhookContextDesignFactory : IDesignTimeDbContextFactory<WebhookDbContext>
{
    public WebhookDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseNpgsql("Host=_;Database=_;Username=_;Password=_");

        return new WebhookDbContext(optionsBuilder.Options);
    }
}
```

- [ ] **Step 2: Verify compile**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj
```

Expected: Build succeeded. (`CreateWebhookMessageCommand.cs` should now compile too since `WebhookDbContext` is defined.)

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 6: Create `WebhookConsumerRepository` and `CreateWebhookMessageCommandHandler`

**Files:**
- Create: `src/People.Infrastructure/Webhooks/WebhookConsumerRepository.cs`
- Create: `src/People.Infrastructure/Webhooks/CreateWebhookMessageCommandHandler.cs`

- [ ] **Step 1: Create `WebhookConsumerRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed class WebhookConsumerRepository : IWebhookConsumerRepository
{
    private readonly WebhookDbContext _dbContext;

    public WebhookConsumerRepository(WebhookDbContext dbContext) =>
        _dbContext = dbContext;

    public Task<WebhookConsumer?> GetAsync(int id, CancellationToken ct) =>
        _dbContext.WebhookConsumers.FindAsync([id], ct).AsTask();

    public IAsyncEnumerable<WebhookConsumer> GetAllAsync(CancellationToken ct) =>
        _dbContext.WebhookConsumers.AsNoTracking().AsAsyncEnumerable();

    public async Task<WebhookConsumer> CreateAsync(WebhookConsumer consumer, CancellationToken ct)
    {
        _dbContext.WebhookConsumers.Add(consumer);
        await _dbContext.SaveChangesAsync(ct);
        return consumer;
    }

    public async Task<WebhookConsumer> UpdateAsync(WebhookConsumer consumer, CancellationToken ct)
    {
        _dbContext.WebhookConsumers.Update(consumer);
        await _dbContext.SaveChangesAsync(ct);
        return consumer;
    }

    public async Task DeleteAsync(int id, CancellationToken ct) =>
        await _dbContext.WebhookConsumers
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(ct);
}
```

- [ ] **Step 2: Compile**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj
```

Expected: Build succeeded.

- [ ] **Step 2: Create `CreateWebhookMessageCommandHandler.cs`**

The handler lives in Infrastructure so it can inject `WebhookDbContext` directly. Mediator discovers it at runtime by scanning loaded assemblies.

```csharp
using Mediator;
using People.Application.Commands.CreateWebhookMessage;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed class CreateWebhookMessageCommandHandler : ICommandHandler<CreateWebhookMessageCommand>
{
    private readonly WebhookDbContext _dbContext;

    public CreateWebhookMessageCommandHandler(WebhookDbContext dbContext) =>
        _dbContext = dbContext;

    public async ValueTask<Unit> Handle(CreateWebhookMessageCommand request, CancellationToken ct)
    {
        var message = new WebhookMessage(request.AccountId, request.Type, request.OccurredAt);
        _dbContext.WebhookMessages.Add(message);
        await _dbContext.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

- [ ] **Step 3: Compile**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj 2>&1 | grep "error"
```

Expected: Build succeeded.

- [ ] **Step 4: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 7: Update `PeopleDbContext` and `WebhookSender`

**Files:**
- Modify: `src/People.Infrastructure/PeopleDbContext.cs`
- Modify: `src/People.Infrastructure/Webhooks/WebhookSender.cs`

- [ ] **Step 1: Remove `Webhook` from `PeopleDbContext.cs`**

Remove the following line from the `DbSet` properties:
```csharp
// DELETE THIS:
public DbSet<Webhook> Webhooks =>
    Set<Webhook>();
```

Remove the following line from `OnModelCreating`:
```csharp
// DELETE THIS:
modelBuilder.ApplyConfiguration(new WebhookSubscriptionEntityTypeConfiguration());
```

Remove the `using People.Application.Providers.Webhooks;` import if it becomes unused.
Remove the `using People.Infrastructure.EntityConfigurations;` import line referencing `WebhookSubscriptionEntityTypeConfiguration` if no other configs are left.

The `OnModelCreating` should look like:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new AccountEntityTypeConfiguration());
    modelBuilder.ApplyConfiguration(new ConfirmationEntityTypeConfiguration());
    modelBuilder.ApplyConfiguration(new EmailEntityTypeConfiguration());
    modelBuilder.ApplyConfiguration(new ExternalConnectionEntityTypeConfiguration());
    modelBuilder.ApplyConfiguration(new OutboxConsumerEntityConfiguration());
    modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());

    base.OnModelCreating(modelBuilder);
}
```

- [ ] **Step 2: Update `WebhookSender.cs` — change `Webhook` to `WebhookConsumer` and add `EnsureSuccessStatusCode`**

Full file after edit:
```csharp
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed partial class WebhookSender(HttpClient httpClient, ILogger<WebhookSender> logger) : IWebhookSender
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task SendAsync(long accountId, DateTime occurredAt, IEnumerable<WebhookConsumer> subscriptions, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new WebhookPayload(accountId, occurredAt), Options);
        return Task.WhenAll(subscriptions.Select(s => SendOneAsync(s, json, ct)));
    }

    private async Task SendOneAsync(WebhookConsumer subscription, string json, CancellationToken ct)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(subscription.DestinationUrl, UriKind.Absolute),
            Method = subscription.Method switch
            {
                WebhookMethod.Post => HttpMethod.Post,
                WebhookMethod.Put => HttpMethod.Put,
                WebhookMethod.Delete => HttpMethod.Delete,
                _ => throw new UnreachableException($"Unknown webhook method {subscription.Method}")
            },
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(subscription.Token))
            request.Headers.TryAddWithoutValidation("X-Elwark-People-Token", subscription.Token);

        WebhookSending(request.Method, request.RequestUri);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        WebhookSent(request.Method, request.RequestUri, response.StatusCode);
    }

    [UsedImplicitly]
    private readonly record struct WebhookPayload(long AccountId, DateTime CreatedAt);

    [LoggerMessage(LogLevel.Information, "Webhook to {Method} {Url} completed with Status Code: {StatusCode}")]
    private partial void WebhookSent(HttpMethod method, Uri url, HttpStatusCode statusCode);

    [LoggerMessage(LogLevel.Information, "Initiating webhook to {Method} {Url}")]
    partial void WebhookSending(HttpMethod method, Uri url);
}
```

- [ ] **Step 3: Compile (will have errors from files using old `Webhook` type — that's expected until Task 14)**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj 2>&1 | grep -c "error"
```

Expected: errors only in `WebhookRetriever.cs` (old file, to be deleted in Task 14).

- [ ] **Step 4: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 8: Handle migrations

**Files:**
- Move: `src/People.Infrastructure/Migrations/` → `src/People.Infrastructure/Migrations/People/`
- Modify: `src/People.Infrastructure/Migrations/People/PeopleDbContextModelSnapshot.cs`
- Create: `src/People.Infrastructure/Migrations/Webhooks/` (via `dotnet ef`)

- [ ] **Step 1: Create the new directory and move files**

```bash
mkdir -p src/People.Infrastructure/Migrations/People
mv src/People.Infrastructure/Migrations/20260403034510_Init.cs src/People.Infrastructure/Migrations/People/
mv src/People.Infrastructure/Migrations/20260403034510_Init.Designer.cs src/People.Infrastructure/Migrations/People/
mv src/People.Infrastructure/Migrations/PeopleDbContextModelSnapshot.cs src/People.Infrastructure/Migrations/People/
```

- [ ] **Step 2: Update namespace in all three moved files**

In `20260403034510_Init.cs`, change:
```csharp
namespace People.Infrastructure.Migrations
```
to:
```csharp
namespace People.Infrastructure.Migrations.People
```

In `20260403034510_Init.Designer.cs`, change:
```csharp
namespace People.Infrastructure.Migrations
```
to:
```csharp
namespace People.Infrastructure.Migrations.People
```

In `PeopleDbContextModelSnapshot.cs`, change:
```csharp
namespace People.Infrastructure.Migrations
```
to:
```csharp
namespace People.Infrastructure.Migrations.People
```

- [ ] **Step 3: Remove `Webhook` entity from `PeopleDbContextModelSnapshot.cs`**

In `src/People.Infrastructure/Migrations/People/PeopleDbContextModelSnapshot.cs`, find and delete the entire block:
```csharp
modelBuilder.Entity("People.Application.Providers.Webhooks.Webhook", b =>
{
    // ... all lines until the closing });
});
```

This block configures the `webhooks` table in the People context snapshot. Removing it tells EF that `PeopleDbContext` no longer owns that table.

- [ ] **Step 4: Verify compile**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj 2>&1 | grep "error" | grep -v "WebhookRetriever"
```

Expected: 0 errors (excluding the old `WebhookRetriever.cs` which is still present).

- [ ] **Step 5: Generate the Webhooks migration**

Run from `src/People.Api/` (the startup project):

```bash
cd src/People.Api && dotnet ef migrations add InitWebhooks \
    --context WebhookDbContext \
    --output-dir ../People.Infrastructure/Migrations/Webhooks \
    -p ../People.Infrastructure
```

Expected: A new file `src/People.Infrastructure/Migrations/Webhooks/YYYYMMDDHHMMSS_InitWebhooks.cs` is created.

- [ ] **Step 6: Edit the generated `InitWebhooks.cs` — remove webhooks table creation**

The generated migration will contain `CREATE TABLE webhooks` and `CREATE TABLE webhook_messages`. Remove the `webhooks` table creation from `Up` because the table already exists (created by `PeopleDbContext`'s `Init` migration).

In `Up`, delete the `migrationBuilder.CreateTable(name: "webhooks", ...)` block and the `migrationBuilder.CreateIndex(name: "IX_webhook_subscriptions_type", ...)` call.

In `Down`, delete the `migrationBuilder.DropTable(name: "webhooks")` call.

Keep everything related to `webhook_messages` as generated. The final `Up` should only create `webhook_messages` and its index. The final `Down` should only drop `webhook_messages`.

- [ ] **Step 7: Verify the edited migration compiles**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj
```

Expected: Build succeeded.

- [ ] **Step 8: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 9: Update `HostingExtensions`

**Files:**
- Modify: `src/People.Infrastructure/HostingExtensions.cs`

- [ ] **Step 1: Add `WebhookDbContext` registration and replace webhook services**

In `HostingExtensions.cs`, find the existing webhook registration block:
```csharp
builder.Services
    .AddScoped<IWebhookRetriever, WebhookRetriever>()
    .AddHttpClient<IWebhookSender, WebhookSender>()
    .AddStandardResilienceHandler();
```

Replace it with:
```csharp
builder.Services
    .AddDbContextFactory<WebhookDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("Postgresql"))
    )
    .AddScoped<IWebhookConsumerRepository, WebhookConsumerRepository>()
    .AddHttpClient<IWebhookSender, WebhookSender>()
    .AddStandardResilienceHandler();
```

Add using if needed:
```csharp
using People.Infrastructure.Webhooks;
```

- [ ] **Step 2: Compile**

```bash
dotnet build src/People.Infrastructure/People.Infrastructure.csproj 2>&1 | grep "error" | grep -v "WebhookRetriever"
```

Expected: 0 errors (excluding old WebhookRetriever).

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 10: Update `OutboxDispatchJob`

**Files:**
- Modify: `src/People.Worker/Jobs/OutboxDispatchJob.cs`

- [ ] **Step 1: Replace `SendWebhooksCommand` with `CreateWebhookMessageCommand`**

Change the `GetCommands` method from:
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
        _ => throw new ArgumentOutOfRangeException()
    };
```

To:
```csharp
private static IEnumerable<ICommand> GetCommands(IIntegrationEvent payload) =>
    payload switch
    {
        AccountCreatedIntegrationEvent x =>
        [
            new EnrichAccountCommand(x.AccountId, x.IpAddress),
            new CreateWebhookMessageCommand(x.AccountId, WebhookType.Created, x.OccurredAt)
        ],
        AccountUpdatedIntegrationEvent x =>
        [
            new CreateWebhookMessageCommand(x.AccountId, WebhookType.Updated, x.OccurredAt)
        ],
        AccountDeletedIntegrationEvent x =>
        [
            new CreateWebhookMessageCommand(x.AccountId, WebhookType.Deleted, x.OccurredAt)
        ],
        _ => throw new ArgumentOutOfRangeException()
    };
```

Update the `using` directives — remove `SendWebhooks`, add `CreateWebhookMessage`:
```csharp
// Remove:
using People.Application.Commands.SendWebhooks;
// Add:
using People.Application.Commands.CreateWebhookMessage;
```

- [ ] **Step 2: Compile the Worker**

```bash
dotnet build src/People.Worker/People.Worker.csproj 2>&1 | grep "error"
```

Expected: 0 errors.

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 11: Create `WebhookDispatchJob`

**Files:**
- Create: `src/People.Worker/Jobs/WebhookDispatchJob.cs`

- [ ] **Step 1: Create `WebhookDispatchJob.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using People.Application.Providers.Webhooks;
using People.Infrastructure.Webhooks;
using Quartz;

namespace People.Worker.Jobs;

[DisallowConcurrentExecution]
public sealed partial class WebhookDispatchJob : IJob
{
    private readonly IDbContextFactory<WebhookDbContext> _dbFactory;
    private readonly IWebhookSender _sender;
    private readonly ILogger<WebhookDispatchJob> _logger;

    public WebhookDispatchJob(
        IDbContextFactory<WebhookDbContext> dbFactory,
        IWebhookSender sender,
        ILogger<WebhookDispatchJob> logger)
    {
        _dbFactory = dbFactory;
        _sender = sender;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var utcNow = context.FireTimeUtc.UtcDateTime;

        await using var db = await _dbFactory.CreateDbContextAsync(context.CancellationToken);

        var messages = await db.WebhookMessages
            .Where(x => x.Status == WebhookMessageStatus.Pending &&
                        (x.RetryAfter == null || x.RetryAfter <= utcNow))
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                var consumers = await db.WebhookConsumers
                    .AsNoTracking()
                    .Where(c => c.Type == message.Type)
                    .ToListAsync(context.CancellationToken);

                if (consumers.Count > 0)
                    await _sender.SendAsync(message.AccountId, message.OccurredAt, consumers, context.CancellationToken);

                db.WebhookMessages.Remove(message);
                MessageSent(message.Id);
            }
            catch (Exception ex)
            {
                message.MarkFailed(utcNow.AddMinutes(1));
                MessageFailed(message.Id, message.Attempts, message.Status, ex);
            }
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    [LoggerMessage(LogLevel.Information, "Webhook message delivered and removed. MessageId={id}")]
    private partial void MessageSent(long id);

    [LoggerMessage(LogLevel.Warning, "Webhook message delivery failed. MessageId={id}, Attempts={attempts}, Status={status}")]
    private partial void MessageFailed(long id, int attempts, WebhookMessageStatus status, Exception ex);
}
```

- [ ] **Step 2: Compile**

```bash
dotnet build src/People.Worker/People.Worker.csproj 2>&1 | grep "error"
```

Expected: 0 errors.

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 12: Register `WebhookDispatchJob` in Worker and apply migrations

**Files:**
- Modify: `src/People.Worker/Program.cs`

- [ ] **Step 1: Add `WebhookDispatchJob` to Quartz schedule in `Program.cs`**

In the `builder.Services.AddQuartz(options => { ... })` block, add after the existing `OutboxDispatchJob` schedule:

```csharp
options.ScheduleJob<WebhookDispatchJob>(trigger => trigger
    .WithIdentity(nameof(WebhookDispatchJob))
    .StartAt(DateBuilder.EvenMinuteDateAfterNow())
    .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever())
);
```

- [ ] **Step 2: Apply `WebhookDbContext` migrations on startup**

In `Program.cs`, find the existing migration block:
```csharp
await using (var scope = host.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

Replace with:
```csharp
await using (var scope = host.Services.CreateAsyncScope())
{
    var peopleDb = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
    await peopleDb.Database.MigrateAsync();

    var webhookDbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WebhookDbContext>>();
    await using var webhookDb = await webhookDbFactory.CreateDbContextAsync();
    await webhookDb.Database.MigrateAsync();
}
```

Add `using People.Infrastructure.Webhooks;` and `using Microsoft.EntityFrameworkCore;` if not present.

- [ ] **Step 3: Compile**

```bash
dotnet build src/People.Worker/People.Worker.csproj 2>&1 | grep "error"
```

Expected: 0 errors.

- [ ] **Step 4: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 13: Add `WebhookEndpoints` and wire up in API

**Files:**
- Create: `src/People.Api/Endpoints/WebhookEndpoints.cs`
- Modify: `src/People.Api/Program.cs`

- [ ] **Step 1: Create `WebhookEndpoints.cs`**

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Filters;
using People.Application.Providers.Webhooks;

namespace People.Api.Endpoints;

internal static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/webhooks")
            .WithTags("Webhooks")
            .RequireAuthorization(Policy.RequireAdmin.Name);

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync)
            .AddEndpointFilter<ValidatorFilter<CreateRequest>>();
        group.MapPut("/{id:int}", UpdateAsync)
            .AddEndpointFilter<ValidatorFilter<UpdateRequest>>();
        group.MapDelete("/{id:int}", DeleteAsync);

        return routes;
    }

    private static IAsyncEnumerable<WebhookResponse> GetAllAsync(
        IWebhookConsumerRepository repository,
        CancellationToken ct) =>
        repository.GetAllAsync(ct).Select(WebhookResponse.Map);

    private static async Task<Results<Ok<WebhookResponse>, NotFound>> GetByIdAsync(
        int id,
        IWebhookConsumerRepository repository,
        CancellationToken ct)
    {
        var consumer = await repository.GetAsync(id, ct);
        return consumer is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(WebhookResponse.Map(consumer));
    }

    private static async Task<WebhookResponse> CreateAsync(
        CreateRequest request,
        IWebhookConsumerRepository repository,
        CancellationToken ct)
    {
        var consumer = new WebhookConsumer(request.Type, request.Method, request.DestinationUrl, request.Token);
        var result = await repository.CreateAsync(consumer, ct);
        return WebhookResponse.Map(result);
    }

    private static async Task<Results<Ok<WebhookResponse>, NotFound>> UpdateAsync(
        int id,
        UpdateRequest request,
        IWebhookConsumerRepository repository,
        CancellationToken ct)
    {
        var consumer = await repository.GetAsync(id, ct);
        if (consumer is null)
            return TypedResults.NotFound();

        consumer.Update(request.Type, request.Method, request.DestinationUrl, request.Token);
        var result = await repository.UpdateAsync(consumer, ct);
        return TypedResults.Ok(WebhookResponse.Map(result));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id,
        IWebhookConsumerRepository repository,
        CancellationToken ct)
    {
        var consumer = await repository.GetAsync(id, ct);
        if (consumer is null)
            return TypedResults.NotFound();

        await repository.DeleteAsync(id, ct);
        return TypedResults.NoContent();
    }

    internal sealed record WebhookResponse(int Id, string Type, string Method, string DestinationUrl, string? Token)
    {
        internal static WebhookResponse Map(WebhookConsumer c) =>
            new(c.Id, c.Type.ToString(), c.Method.ToString(), c.DestinationUrl, c.Token);
    }

    internal sealed record CreateRequest(WebhookType Type, WebhookMethod Method, string DestinationUrl, string? Token)
    {
        internal sealed class Validator : AbstractValidator<CreateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();
                RuleFor(x => x.Method).IsInEnum();
                RuleFor(x => x.DestinationUrl)
                    .NotEmpty()
                    .MaximumLength(2048)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                    .WithMessage("Must be a valid absolute URL.");
                RuleFor(x => x.Token).MaximumLength(256);
            }
        }
    }

    internal sealed record UpdateRequest(WebhookType Type, WebhookMethod Method, string DestinationUrl, string? Token)
    {
        internal sealed class Validator : AbstractValidator<UpdateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();
                RuleFor(x => x.Method).IsInEnum();
                RuleFor(x => x.DestinationUrl)
                    .NotEmpty()
                    .MaximumLength(2048)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                    .WithMessage("Must be a valid absolute URL.");
                RuleFor(x => x.Token).MaximumLength(256);
            }
        }
    }
}
```

- [ ] **Step 2: Register in `Program.cs`**

In `src/People.Api/Program.cs`, find where `MapAccountEndpoints` or similar is called and add:
```csharp
app.MapWebhookEndpoints();
```

Also apply `WebhookDbContext` migrations on startup (if People.Api has a migration startup block — check the file). If it does, add:
```csharp
await using var webhookDbFactory = app.Services.GetRequiredService<IDbContextFactory<WebhookDbContext>>();
await using var webhookDb = await webhookDbFactory.CreateDbContextAsync();
await webhookDb.Database.MigrateAsync();
```

- [ ] **Step 3: Compile**

```bash
dotnet build src/People.Api/People.Api.csproj 2>&1 | grep "error"
```

Expected: 0 errors.

- [ ] **Step 4: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 14: Update `add_migration.sh`

**Files:**
- Modify: `src/People.Api/add_migration.sh`

- [ ] **Step 1: Replace the script content**

Full new content of `src/People.Api/add_migration.sh`:
```bash
#!/bin/bash
set -euo pipefail

NAME="${1:?Usage: ./add_migration.sh <MigrationName>}"

for CONTEXT in PeopleDbContext WebhookDbContext; do
  if [ "$CONTEXT" = "PeopleDbContext" ]; then
    DIR="Migrations/People"
  else
    DIR="Migrations/Webhooks"
  fi

  if dotnet ef migrations has-pending-model-changes \
      --context "$CONTEXT" \
      -p ../People.Infrastructure \
      -s . \
      --no-build \
      -q 2>/dev/null; then
    echo "Creating migration '$NAME' for $CONTEXT → $DIR"
    dotnet ef migrations add "$NAME" \
        --context "$CONTEXT" \
        --output-dir "../People.Infrastructure/$DIR" \
        -p ../People.Infrastructure \
        -s .
  else
    echo "No model changes in $CONTEXT — skipping"
  fi
done
```

- [ ] **Step 2: Make it executable**

```bash
chmod +x src/People.Api/add_migration.sh
```

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 15: Delete obsolete files

**Files to delete:**
- `src/People.Application/Providers/Webhooks/Webhook.cs`
- `src/People.Application/Providers/Webhooks/IWebhookRetriever.cs`
- `src/People.Application/Commands/SendWebhooks/SendWebhooksCommand.cs`
- `src/People.Infrastructure/Webhooks/WebhookRetriever.cs`
- `src/People.Infrastructure/EntityConfigurations/WebhookSubscriptionEntityTypeConfiguration.cs`

- [ ] **Step 1: Delete the files**

```bash
rm src/People.Application/Providers/Webhooks/Webhook.cs
rm src/People.Application/Providers/Webhooks/IWebhookRetriever.cs
rm src/People.Application/Commands/SendWebhooks/SendWebhooksCommand.cs
rm src/People.Infrastructure/Webhooks/WebhookRetriever.cs
rm src/People.Infrastructure/EntityConfigurations/WebhookSubscriptionEntityTypeConfiguration.cs
```

- [ ] **Step 2: Full solution build**

```bash
dotnet build Elwark.People.slnx 2>&1 | grep -E "^.*error"
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 16: Update test infrastructure

**Files:**
- Modify: `tests/Integration/People.IntegrationTests/Infrastructure/PostgreSqlFixture.cs`
- Modify: `tests/Integration/People.IntegrationTests/Commands/CommandTestFixture.cs`

- [ ] **Step 1: Add `WebhookDbContext` migration to `PostgreSqlFixture.InitializeAsync`**

In `PostgreSqlFixture.cs`, update `InitializeAsync` to also migrate `WebhookDbContext`:

```csharp
public async Task InitializeAsync()
{
    await _container.StartAsync();

    await using var ctx = CreateContext();
    await ctx.Database.MigrateAsync();

    var webhookOptions = new DbContextOptionsBuilder<WebhookDbContext>()
        .UseNpgsql(ConnectionString)
        .Options;
    await using var webhookCtx = new WebhookDbContext(webhookOptions);
    await webhookCtx.Database.MigrateAsync();
}
```

Add `using People.Infrastructure.Webhooks;` and `using Microsoft.EntityFrameworkCore;` if not present.

Also add a helper method for tests:
```csharp
public WebhookDbContext CreateWebhookContext()
{
    var options = new DbContextOptionsBuilder<WebhookDbContext>()
        .UseNpgsql(ConnectionString)
        .Options;
    return new WebhookDbContext(options);
}
```

- [ ] **Step 2: Add `WebhookDbContext` to `CommandTestFixture` DI**

In `CommandTestFixture.InitializeAsync`, find the `AddDbContextFactory<PeopleDbContext>` registration and add after it:

```csharp
services.AddDbContextFactory<WebhookDbContext>(options =>
    options.UseNpgsql(_postgres.ConnectionString)
);
```

Add `using People.Infrastructure.Webhooks;` if not present.

- [ ] **Step 3: Compile tests**

```bash
dotnet build tests/Integration/People.IntegrationTests/People.IntegrationTests.csproj 2>&1 | grep -E "error"
```

Expected: errors only from `SendWebhooksCommandTests.cs` which will be deleted next.

- [ ] **Step 4: Delete `SendWebhooksCommandTests.cs`**

```bash
rm tests/Integration/People.IntegrationTests/Commands/SendWebhooksCommandTests.cs
```

- [ ] **Step 5: Compile again**

```bash
dotnet build tests/Integration/People.IntegrationTests/People.IntegrationTests.csproj 2>&1 | grep "error"
```

Expected: 0 errors.

- [ ] **Step 6: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 17: Write `WebhookMessage` unit tests

**Files:**
- Create: `tests/Unit/People.UnitTests/WebhookMessageTests.cs` (adjust path to match existing unit test project structure)

- [ ] **Step 1: Write failing tests**

```csharp
using People.Application.Providers.Webhooks;
using Xunit;

namespace People.UnitTests;

public sealed class WebhookMessageTests
{
    [Fact]
    public void MarkFailed_FirstFailure_IncrementsAttemptsAndSetsRetryAfter()
    {
        var message = new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow);
        var retryAfter = DateTime.UtcNow.AddMinutes(1);

        message.MarkFailed(retryAfter);

        Assert.Equal(1, message.Attempts);
        Assert.Equal(WebhookMessageStatus.Pending, message.Status);
        Assert.Equal(retryAfter, message.RetryAfter);
    }

    [Fact]
    public void MarkFailed_NinthFailure_StillPending()
    {
        var message = new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow);
        var retryAfter = DateTime.UtcNow.AddMinutes(1);

        for (var i = 0; i < 9; i++)
            message.MarkFailed(retryAfter);

        Assert.Equal(9, message.Attempts);
        Assert.Equal(WebhookMessageStatus.Pending, message.Status);
    }

    [Fact]
    public void MarkFailed_TenthFailure_SetsFailedStatus()
    {
        var message = new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow);
        var retryAfter = DateTime.UtcNow.AddMinutes(1);

        for (var i = 0; i < 10; i++)
            message.MarkFailed(retryAfter);

        Assert.Equal(10, message.Attempts);
        Assert.Equal(WebhookMessageStatus.Failed, message.Status);
    }
}
```

- [ ] **Step 2: Run the tests**

```bash
dotnet test tests/Unit/People.UnitTests/ --filter "WebhookMessageTests" -v normal
```

Expected: All 3 tests PASS.

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 18: Write `CreateWebhookMessageCommand` integration tests

**Files:**
- Create: `tests/Integration/People.IntegrationTests/Commands/CreateWebhookMessageCommandTests.cs`

- [ ] **Step 1: Write the failing test file**

```csharp
using Microsoft.EntityFrameworkCore;
using People.Application.Commands.CreateWebhookMessage;
using People.Application.Providers.Webhooks;
using People.Infrastructure.Webhooks;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class CreateWebhookMessageCommandTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Handle_CreatesWebhookMessageRow()
    {
        await using var webhookDb = fixture.CreateWebhookContext();
        await webhookDb.WebhookMessages.ExecuteDeleteAsync();

        var occurred = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var command = new CreateWebhookMessageCommand(42L, WebhookType.Created, occurred);
        var handler = new CreateWebhookMessageCommandHandler(webhookDb);

        await handler.Handle(command, CancellationToken.None);

        var messages = await webhookDb.WebhookMessages.ToListAsync();
        Assert.Single(messages);
        Assert.Equal(42L, messages[0].AccountId);
        Assert.Equal(WebhookType.Created, messages[0].Type);
        Assert.Equal(occurred, messages[0].OccurredAt);
        Assert.Equal(0, messages[0].Attempts);
        Assert.Null(messages[0].RetryAfter);
        Assert.Equal(WebhookMessageStatus.Pending, messages[0].Status);
    }

    [Fact]
    public async Task Handle_DifferentTypes_EachCreatesOwnRow()
    {
        await using var webhookDb = fixture.CreateWebhookContext();
        await webhookDb.WebhookMessages.ExecuteDeleteAsync();

        var occurred = DateTime.UtcNow;
        var handler = new CreateWebhookMessageCommandHandler(webhookDb);

        await handler.Handle(new CreateWebhookMessageCommand(1L, WebhookType.Created, occurred), CancellationToken.None);
        await handler.Handle(new CreateWebhookMessageCommand(2L, WebhookType.Updated, occurred), CancellationToken.None);
        await handler.Handle(new CreateWebhookMessageCommand(3L, WebhookType.Deleted, occurred), CancellationToken.None);

        var count = await webhookDb.WebhookMessages.CountAsync();
        Assert.Equal(3, count);
    }
}
```

- [ ] **Step 2: Run the tests**

```bash
dotnet test tests/Integration/People.IntegrationTests/People.IntegrationTests.csproj \
    --filter "CreateWebhookMessageCommandTests" \
    -v normal
```

Expected: Both tests PASS.

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 18: Write `WebhookDispatchJob` integration tests

**Files:**
- Create: `tests/Integration/People.IntegrationTests/Jobs/WebhookDispatchJobTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using People.Application.Providers.Webhooks;
using People.Infrastructure.Webhooks;
using People.IntegrationTests.Infrastructure;
using Quartz;
using Xunit;

namespace People.IntegrationTests.Jobs;

[Collection(nameof(PostgresCollection))]
public sealed class WebhookDispatchJobTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Execute_WhenAllConsumersSucceed_DeletesMessage()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.WebhookMessages.ExecuteDeleteAsync();
        await db.WebhookConsumers.ExecuteDeleteAsync();

        var consumer = new WebhookConsumer(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null);
        db.WebhookConsumers.Add(consumer);
        var message = new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow);
        db.WebhookMessages.Add(message);
        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        var job = CreateJob(db, sender);
        var context = CreateJobContext();

        await job.Execute(context);

        var remaining = await db.WebhookMessages.CountAsync();
        Assert.Equal(0, remaining);
        await sender.Received(1).SendAsync(
            1L,
            Arg.Any<DateTime>(),
            Arg.Is<IEnumerable<WebhookConsumer>>(list => list.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenSendFails_IncrementsAttemptsAndSetsRetryAfter()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.WebhookMessages.ExecuteDeleteAsync();
        await db.WebhookConsumers.ExecuteDeleteAsync();

        db.WebhookConsumers.Add(new WebhookConsumer(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null));
        db.WebhookMessages.Add(new WebhookMessage(2L, WebhookType.Created, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        sender.SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<WebhookConsumer>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var job = CreateJob(db, sender);
        await job.Execute(CreateJobContext());

        var msg = await db.WebhookMessages.SingleAsync();
        Assert.Equal(1, msg.Attempts);
        Assert.Equal(WebhookMessageStatus.Pending, msg.Status);
        Assert.NotNull(msg.RetryAfter);
    }

    // The 10-attempt threshold logic is tested directly as a unit test on WebhookMessage.MarkFailed
    // (see WebhookMessageTests). The integration test below verifies the job correctly persists
    // attempt count and retry_after after a single failure.

    [Fact]
    public async Task Execute_WhenNoMatchingConsumers_DeletesMessageWithoutSending()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.WebhookMessages.ExecuteDeleteAsync();
        await db.WebhookConsumers.ExecuteDeleteAsync();

        // Consumer for Created, but message is for Deleted
        db.WebhookConsumers.Add(new WebhookConsumer(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null));
        db.WebhookMessages.Add(new WebhookMessage(4L, WebhookType.Deleted, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        var job = CreateJob(db, sender);
        await job.Execute(CreateJobContext());

        var remaining = await db.WebhookMessages.CountAsync();
        Assert.Equal(0, remaining);
        await sender.DidNotReceive()
            .SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<WebhookConsumer>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_MessageWithFutureRetryAfter_IsSkipped()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.WebhookMessages.ExecuteDeleteAsync();
        await db.WebhookConsumers.ExecuteDeleteAsync();

        // Manually create a message with a future retry_after by failing it once, then checking it's not re-sent yet
        db.WebhookConsumers.Add(new WebhookConsumer(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null));
        db.WebhookMessages.Add(new WebhookMessage(5L, WebhookType.Created, DateTime.UtcNow));
        await db.SaveChangesAsync();

        // First run - will fail and set RetryAfter to now + 1 minute
        var sender = Substitute.For<IWebhookSender>();
        sender.SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<WebhookConsumer>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("error"));

        var job = CreateJob(db, sender);
        await job.Execute(CreateJobContext());

        // Now second run immediately - message has a future RetryAfter so must be skipped
        sender.ClearReceivedCalls();
        sender.SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<WebhookConsumer>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await job.Execute(CreateJobContext());

        // Still 1 message, not deleted
        var count = await db.WebhookMessages.CountAsync();
        Assert.Equal(1, count);
        await sender.DidNotReceive()
            .SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<WebhookConsumer>>(), Arg.Any<CancellationToken>());
    }

    private WebhookDispatchJob CreateJob(WebhookDbContext db, IWebhookSender sender)
    {
        var factory = Substitute.For<IDbContextFactory<WebhookDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(db);

        return new WebhookDispatchJob(
            factory,
            sender,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WebhookDispatchJob>.Instance
        );
    }

    private static IJobExecutionContext CreateJobContext()
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.FireTimeUtc.Returns(DateTimeOffset.UtcNow);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}
```

- [ ] **Step 2: Run the tests**

```bash
dotnet test tests/Integration/People.IntegrationTests/People.IntegrationTests.csproj \
    --filter "WebhookDispatchJobTests" \
    -v normal
```

Expected: All 5 tests PASS.

- [ ] **Step 3: Run all solution tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass.

---

### Task 19: Full solution test run

- [ ] **Step 1: Run all tests**

```bash
dotnet test Elwark.People.slnx -v minimal
```

Expected: All tests pass. Zero failures.

- [ ] **Step 2: Full solution build to confirm clean state**

```bash
dotnet build Elwark.People.slnx
```

Expected: Build succeeded, 0 warnings about missing references.

- [ ] **Step 3: Final commit**

```bash
git add -A
git status  # confirm nothing unexpected
git commit -m "feat: complete webhook DbContext separation with delivery queue and admin CRUD endpoints"
```
