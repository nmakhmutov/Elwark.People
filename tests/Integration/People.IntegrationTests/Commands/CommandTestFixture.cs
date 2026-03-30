using System.Net;
using System.Net.Mail;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using People.Api.Application.Behaviour;
using People.Api.Grpc;
using People.Api.Infrastructure.Notifications;
using People.Api.Infrastructure.Providers;
using People.Api.Infrastructure.Providers.Google;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Api.Infrastructure.Providers.World;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Domain.SeedWork;
using People.IntegrationTests.Infrastructure;
using People.Kafka.Integration;
using Xunit;

namespace People.IntegrationTests.Commands;

/// <summary>
/// Builds a <see cref="ServiceProvider"/> with real PostgreSQL, Mediator handlers, and mocked external integrations.
/// </summary>
public sealed class CommandTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private ServiceProvider? _provider;

    public CommandTestFixture(PostgreSqlFixture postgres) =>
        _postgres = postgres;

    internal IGoogleApiService Google { get; } = Substitute.For<IGoogleApiService>();

    internal IMicrosoftApiService Microsoft { get; } = Substitute.For<IMicrosoftApiService>();

    public IGravatarService Gravatar { get; } = Substitute.For<IGravatarService>();

    public IIpService IpService { get; } = Substitute.For<IIpService>();

    public INotificationSender Notification { get; } = Substitute.For<INotificationSender>();

    public IIntegrationEventBus EventBus { get; } = Substitute.For<IIntegrationEventBus>();

    internal ICountryClient Country { get; } = Substitute.For<ICountryClient>();

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddHybridCache();

        services.AddSingleton(Google);
        services.AddSingleton(Microsoft);
        services.AddSingleton(Gravatar);
        services.AddSingleton(IpService);
        services.AddSingleton(Notification);
        services.AddSingleton(EventBus);
        services.AddSingleton(Country);
        services.AddSingleton<IEnumerable<IIpService>>(sp => [sp.GetRequiredService<IIpService>()]);

        services.AddInfrastructure(options =>
        {
            options.PostgresqlConnectionString = _postgres.ConnectionString;
            options.AppKey = new string('K', 32);
            options.AppVector = new string('V', 16);
        });

        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(RequestLoggingBehavior<,>), typeof(RequestValidatorBehavior<,>)];
        });

        services.AddValidatorsFromAssembly(
            typeof(PeopleService).Assembly,
            filter: null,
            includeInternalTypes: true
        );

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        IpService.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((IpInformation?)null);
        Gravatar.GetAsync(Arg.Any<MailAddress>()).Returns((GravatarProfile?)null);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
            await _provider.DisposeAsync();
    }

    public IServiceScope CreateScope() =>
        _provider?.CreateScope() ?? throw new InvalidOperationException("Fixture not initialized.");

    /// <summary>Clears application tables (same order as <see cref="IntegrationDatabaseCleanup"/>).</summary>
    public static async Task ResetDatabaseAsync(PeopleDbContext db, CancellationToken ct = default) =>
        await IntegrationDatabaseCleanup.DeleteAllAsync(db, ct);

    /// <summary>Persist a new account with a single confirmed primary email (no external sign-in).</summary>
    public static async Task<AccountId> SeedAccountWithConfirmedEmailAsync(
        IServiceScope scope,
        MailAddress email,
        string nickname = "seed-user",
        CancellationToken ct = default
    )
    {
        var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var account = Account.Create(nickname, Language.Parse("en"), IPAddress.Loopback, hasher);
        account.ClearDomainEvents();
        account.AddEmail(email, true, time);

        await repo.AddAsync(account, ct);
        await repo.UnitOfWork.SaveEntitiesAsync(ct);

        return account.Id;
    }

    public PeopleDbContext CreateReadOnlyContext() =>
        _postgres.CreateContext(new NoOpMediator());
}
