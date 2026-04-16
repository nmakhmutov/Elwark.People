using System.Net;
using System.Net.Mail;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using People.Application.Behaviour;
using People.Api.Grpc;
using People.Application.Providers;
using People.Application.Providers.Country;
using People.Application.Providers.Google;
using People.Application.Providers.Gravatar;
using People.Application.Providers.Ip;
using People.Application.Providers.Confirmation;
using People.Application.Providers.Microsoft;
using People.Application.Providers.Postgres;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Cryptography;
using People.Infrastructure.Mappers;
using People.Infrastructure.Outbox.Extensions;
using People.Infrastructure.Providers.Postgres;
using People.Infrastructure.Repositories;
using Xunit;

namespace Integration.Api.Tests.Commands;

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
        services.AddSingleton(Country);
        services.AddSingleton<IEnumerable<IIpService>>(sp => [sp.GetRequiredService<IIpService>()]);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IIpHasher, IpHasher>();
        services.AddSingleton(Options.Create(new AppSecurityOptions(new string('K', 32), new string('V', 16))));

        services.AddOutbox<PeopleDbContext>(outbox => outbox
            .AddMapper(new AccountCreatedMapper())
            .AddMapper(new AccountUpdatedMapper())
            .AddMapper(new AccountDeletedMapper())
            .AddMapper(new EmailVerificationRequestedMapper())
        );

        services.AddSingleton<INpgsqlAccessor>(new NpgsqlAccessor(
            _postgres.ConnectionString,
            NullLoggerFactory.Instance));

        services.AddDbContextFactory<PeopleDbContext>(options =>
            options.UseNpgsql(
                _postgres.ConnectionString,
                npgsql => npgsql.ConfigureDataSource(ds => ds.EnableDynamicJson())
            )
        );
        services.AddDbContextFactory<WebhookDbContext>(options =>
            options.UseNpgsql(_postgres.ConnectionString)
        );

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IConfirmationChallengeService, ConfirmationChallengeService>();
        services.AddScoped<IEmailVerificationTokenService, EmailVerificationTokenService>();

        MediatorDependencyInjectionExtensions.AddMediator(
            services,
            options =>
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
        CancellationToken ct = default,
        string nickname = "integration"
    )
    {
        var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IIpHasher>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var account = Account.Create(Timezone.Utc, AccountTestFactory.EnLocale, IPAddress.Loopback, hasher, time);
        account.Update(
            Name.Create(Nickname.Parse(nickname)),
            account.Picture,
            account.Locale,
            account.Region,
            account.Country,
            account.Timezone,
            time
            );
        account.ClearDomainEvents();
        account.AddEmail(email, true, time);

        await repo.AddAsync(account, ct);
        await repo.UnitOfWork.SaveEntitiesAsync(ct);

        return account.Id;
    }

    public PeopleDbContext CreateReadOnlyContext() =>
        _postgres.CreateContext();
}
