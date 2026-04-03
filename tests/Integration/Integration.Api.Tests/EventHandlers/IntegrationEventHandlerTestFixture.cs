using System.Net.Mail;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using People.Application.Behaviour;
using People.Application.Commands.EnrichAccount;
using People.Api.Grpc;
using People.Application.Providers.Confirmation;
using People.Application.Providers.Gravatar;
using People.Application.Providers.Ip;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Cryptography;
using People.Infrastructure.Mappers;
using People.Infrastructure.Outbox.Extensions;
using People.Infrastructure.Repositories;
using Xunit;

namespace Integration.Api.Tests.EventHandlers;

/// <summary>
/// Real PostgreSQL + infrastructure with mocked external integrations for integration event handlers.
/// </summary>
public sealed class IntegrationEventHandlerTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private ServiceProvider? _provider;

    public IntegrationEventHandlerTestFixture(PostgreSqlFixture postgres) =>
        _postgres = postgres;

    public IConfirmationService Confirmation { get; } = Substitute.For<IConfirmationService>();

    public IGravatarService Gravatar { get; } = Substitute.For<IGravatarService>();

    public IIpService Ip1 { get; } = Substitute.For<IIpService>();

    public IIpService Ip2 { get; } = Substitute.For<IIpService>();

    public Task InitializeAsync()
    {
        ResetExternalMocks();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddHybridCache();

        services.AddSingleton(Gravatar);
        services.AddSingleton<IEnumerable<IIpService>>(_ => [Ip1, Ip2]);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IIpHasher, IpHasher>();
        services.AddSingleton<IOptions<AppSecurityOptions>>(
            Options.Create(new AppSecurityOptions(new string('K', 32), new string('V', 16))));

        services.AddOutbox<PeopleDbContext>(outbox => outbox
            .AddMapper(new AccountCreatedMapper())
            .AddMapper(new AccountUpdatedMapper())
            .AddMapper(new AccountDeletedMapper())
        );

        services.AddDbContextFactory<PeopleDbContext>(options =>
            options.UseNpgsql(
                _postgres.ConnectionString,
                npgsql => npgsql.ConfigureDataSource(ds => ds.EnableDynamicJson())
            )
        );

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IConfirmationService, ConfirmationService>();

        foreach (var d in services.Where(d => d.ServiceType == typeof(IConfirmationService)).ToList())
            services.Remove(d);

        services.AddScoped<IConfirmationService>(_ => Confirmation);

        services.AddScoped<EnrichAccountCommandHandler>();

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

        return Task.CompletedTask;
    }

    public void ResetExternalMocks()
    {
        Ip1.ClearReceivedCalls();
        Ip2.ClearReceivedCalls();
        Gravatar.ClearReceivedCalls();
        Confirmation.ClearReceivedCalls();

        Ip1.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((IpInformation?)null);
        Ip2.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((IpInformation?)null);
        Gravatar.GetAsync(Arg.Any<MailAddress>()).Returns((GravatarProfile?)null);
        Confirmation.DeleteAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(0);
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
            await _provider.DisposeAsync();
    }

    public IServiceScope CreateScope() =>
        _provider?.CreateScope() ?? throw new InvalidOperationException("Fixture not initialized.");

    public PeopleDbContext CreateReadOnlyContext() =>
        _postgres.CreateContext();

    public static async Task<DateTime> QueryAccountTimestampUtcAsync(
        PeopleDbContext db,
        long accountId,
        string column,
        CancellationToken ct = default
    )
    {
        if (column is not ("last_log_in" or "updated_at"))
            throw new ArgumentOutOfRangeException(nameof(column));

        await db.Database.OpenConnectionAsync(ct);
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = $"SELECT {column} FROM accounts WHERE id = @id";
        var p = cmd.CreateParameter();
        p.ParameterName = "id";
        p.Value = accountId;
        cmd.Parameters.Add(p);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DateTime dt
            ? dt
            : throw new InvalidOperationException($"No row or null {column} for account {accountId}.");
    }
}
