using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Cryptography;
using People.Infrastructure.Providers;
using People.Infrastructure.Providers.NpgsqlData;
using People.Infrastructure.Repositories;
using StackExchange.Redis;

namespace People.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        Action<InfrastructureOptions> configure)
    {
        var options = new InfrastructureOptions();
        configure.Invoke(options);

        services
            .AddDbContext<PeopleDbContext>(builder => builder.UseNpgsql(options.PostgresqlConnectionString))
            .AddScoped<IAccountRepository, AccountRepository>()
            .AddScoped<IConfirmationService, ConfirmationService>()
            .AddSingleton<ITimeProvider, TimeProvider>()
            .AddSingleton<IIpHasher, IpHasher>()
            .AddSingleton<INpgsqlDataProvider>(_ => new NpgsqlDataProvider(options.PostgresqlConnectionString))
            .AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(options.RedisConnectionString, true))
            )
            .AddSingleton<IOptions<AppSecurityOptions>>(_ =>
                new OptionsWrapper<AppSecurityOptions>(new AppSecurityOptions(options.AppKey, options.AppVector))
            );

        return services;
    }
}

public sealed record InfrastructureOptions
{
    public string PostgresqlConnectionString { get; set; } = string.Empty;

    public string RedisConnectionString { get; set; } = string.Empty;

    public string AppKey { get; set; } = string.Empty;

    public string AppVector { get; set; } = string.Empty;
}

public sealed record AppSecurityOptions
{
    public AppSecurityOptions(string appKey, string appVector)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("Value cannot be null or empty.", nameof(appKey));

        if (string.IsNullOrWhiteSpace(appVector))
            throw new ArgumentException("Value cannot be null or empty.", nameof(appVector));

        AppKey = Encoding.UTF8.GetBytes(appKey);
        AppVector = Encoding.UTF8.GetBytes(appVector);
    }

    public byte[] AppKey { get; set; }

    public byte[] AppVector { get; set; }
}
