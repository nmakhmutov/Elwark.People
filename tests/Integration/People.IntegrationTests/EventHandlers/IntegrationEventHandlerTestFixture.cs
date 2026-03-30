using System.Net.Mail;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using People.Api.Application.Behaviour;
using People.Api.Application.IntegrationEvents.EventHandling;
using People.Api.Grpc;
using People.Api.Infrastructure.Providers;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Domain.Entities;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.IntegrationTests.Infrastructure;
using People.Kafka.Integration;
using Xunit;

namespace People.IntegrationTests.EventHandlers;

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

    public IIntegrationEventBus EventBus { get; } = Substitute.For<IIntegrationEventBus>();

    public Task InitializeAsync()
    {
        ResetExternalMocks();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddHybridCache();

        services.AddSingleton(Gravatar);
        services.AddSingleton(EventBus);
        services.AddSingleton<IEnumerable<IIpService>>(_ => [Ip1, Ip2]);

        services.AddInfrastructure(options =>
        {
            options.PostgresqlConnectionString = _postgres.ConnectionString;
            options.AppKey = new string('K', 32);
            options.AppVector = new string('V', 16);
        });

        foreach (var d in services.Where(d => d.ServiceType == typeof(IConfirmationService)).ToList())
            services.Remove(d);

        services.AddScoped<IConfirmationService>(_ => Confirmation);

        services.AddScoped<AccountCreatedIntegrationEventHandler>();
        services.AddScoped<AccountEngagedIntegrationEventHandler>();

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

        return Task.CompletedTask;
    }

    public void ResetExternalMocks()
    {
        Ip1.ClearReceivedCalls();
        Ip2.ClearReceivedCalls();
        Gravatar.ClearReceivedCalls();
        Confirmation.ClearReceivedCalls();
        EventBus.ClearReceivedCalls();

        EventBus.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        EventBus
            .PublishAsync(Arg.Any<ICollection<IIntegrationEvent>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

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
        _postgres.CreateContext(new NoOpMediator());

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
