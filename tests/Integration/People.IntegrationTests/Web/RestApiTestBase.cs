using System.Net;
using System.Net.Mail;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Web;

/// <summary>Shared PostgreSQL fixture + <see cref="PeopleApiFactory"/> for REST integration tests.</summary>
[Collection(nameof(PostgresCollection))]
public abstract class RestApiTestBase : IAsyncLifetime
{
    protected RestApiTestBase(PostgreSqlFixture postgres)
    {
        Postgres = postgres;
        Factory = new PeopleApiFactory(postgres);
        Factory.SetupDefaultIntegrationMocks();
    }

    protected PostgreSqlFixture Postgres { get; }

    protected PeopleApiFactory Factory { get; }

    public virtual Task InitializeAsync() =>
        Task.CompletedTask;

    public virtual async Task DisposeAsync() =>
        await Factory.DisposeAsync();

    protected Task ResetAsync() =>
        Factory.ResetDatabaseAsync();

    /// <summary>Creates an account with one confirmed primary email (matches command/query integration seeds).</summary>
    protected static async Task<long> SeedAccountWithConfirmedPrimaryAsync(
        PeopleApiFactory factory,
        MailAddress primaryEmail,
        CancellationToken ct = default
    )
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, time);
        account.ClearDomainEvents();
        account.AddEmail(primaryEmail, true, time);

        await repo.AddAsync(account, ct);
        await repo.UnitOfWork.SaveEntitiesAsync(ct);

        return account.Id;
    }

    protected static async Task<long> SeedAccountWithGoogleConnectionAsync(
        PeopleApiFactory factory,
        MailAddress primaryEmail,
        string googleIdentity,
        CancellationToken ct = default
    )
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, time);
        account.ClearDomainEvents();
        account.AddEmail(primaryEmail, true, time);
        account.AddGoogle(googleIdentity, "G", "User", null, time);

        await repo.AddAsync(account, ct);
        await repo.UnitOfWork.SaveEntitiesAsync(ct);

        return account.Id;
    }

    protected static async Task<long> SeedAccountWithMicrosoftConnectionAsync(
        PeopleApiFactory factory,
        MailAddress primaryEmail,
        string msIdentity,
        string nickname = "ms-user",
        CancellationToken ct = default
    )
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, time);
        account.ClearDomainEvents();
        account.AddEmail(primaryEmail, true, time);
        account.AddMicrosoft(msIdentity, "M", "User", time);

        await repo.AddAsync(account, ct);
        await repo.UnitOfWork.SaveEntitiesAsync(ct);

        return account.Id;
    }

    protected static void AssertUnauthorizedOrForbidden(HttpStatusCode code) =>
        Assert.True(code is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, $"Unexpected {code}");
}
