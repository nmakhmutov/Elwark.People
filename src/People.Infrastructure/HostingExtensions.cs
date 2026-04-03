using System.Globalization;
using System.Text;
using Duende.AccessTokenManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Application.Providers.Country;
using People.Application.Providers.Google;
using People.Application.Providers.Gravatar;
using People.Application.Providers.Ip;
using People.Application.Providers.Microsoft;
using People.Application.Providers.Postgres;
using People.Application.Providers.Webhooks;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Cryptography;
using People.Infrastructure.EmailBuilder;
using People.Infrastructure.Mappers;
using People.Infrastructure.Outbox.Extensions;
using People.Infrastructure.Providers;
using People.Infrastructure.Providers.Ip;
using People.Infrastructure.Providers.Postgres;
using People.Infrastructure.Repositories;
using People.Infrastructure.Webhooks;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace People.Infrastructure;

public static class HostingExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IServiceCollection AddInfrastructure()
        {
            var configuration = builder.Configuration;

            builder.Services
                .AddOutbox<PeopleDbContext>(outbox => outbox
                    .AddMapper(new AccountCreatedMapper())
                    .AddMapper(new AccountUpdatedMapper())
                    .AddMapper(new AccountDeletedMapper())
                );

            builder.Services
                .AddHybridCache();

            builder.Services
                .AddSingleton(TimeProvider.System)
                .AddDbContextFactory<PeopleDbContext>(options =>
                    options.UseNpgsql(
                        configuration.GetConnectionString("Postgresql"),
                        npgsql => npgsql.ConfigureDataSource(ds => ds.EnableDynamicJson())
                    )
                )
                .AddScoped<IAccountRepository, AccountRepository>()
                .AddScoped<IConfirmationService, ConfirmationService>()
                .AddSingleton<IIpHasher, IpHasher>()
                .AddSingleton<INpgsqlAccessor>(provider => new NpgsqlAccessor(
                        configuration.GetConnectionString("Postgresql")!,
                        provider.GetRequiredService<ILoggerFactory>()
                    )
                )
                .AddSingleton<IEmailBuilder, EmailBuilder.EmailBuilder>();

            builder.Services
                .AddSingleton<IOptions<AppSecurityOptions>>(_ =>
                    Options.Create(new AppSecurityOptions(configuration["App:Key"]!, configuration["App:Vector"]!))
                );

            builder.Services
                .AddClientCredentialsTokenManagement()
                .AddClient(ClientCredentialsClientName.Parse("notification"), client =>
                {
                    client.TokenEndpoint = configuration.GetUri("Authentication:Authority", "connect/token");
                    client.ClientId = ClientId.Parse(configuration.GetString("Notification:ClientId"));
                    client.ClientSecret = ClientSecret.Parse(configuration.GetString("Notification:ClientSecret"));
                    client.Scope = Scope.Parse(configuration.GetString("Notification:Scope"));
                });

            builder.Services
                .AddHttpClient<INotificationSender, NotificationSender>(client =>
                    client.BaseAddress = new Uri(configuration["Notification:Host"]!)
                )
                .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("notification"));

            builder.Services
                .AddHttpClient<ICountryClient, CountryClient>(client =>
                    client.BaseAddress = new Uri(configuration["Urls:Countries.Api"]!)
                );

            builder.Services
                .AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
                    client.BaseAddress = new Uri(configuration["Urls:Google.Api"]!)
                );

            builder.Services
                .AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
                    client.BaseAddress = new Uri(configuration["Urls:Microsoft.Api"]!)
                );

            builder.Services
                .AddHttpClient<IIpService, IpApiService>(client =>
                    client.DefaultRequestHeaders.Add("User-Agent", configuration["UserAgent"])
                );

            builder.Services
                .AddHttpClient<IIpService, GeoPluginService>(client =>
                    client.DefaultRequestHeaders.Add("User-Agent", configuration["UserAgent"])
                );

            builder.Services
                .AddHttpClient<IIpService, IpQueryService>(client =>
                    client.DefaultRequestHeaders.Add("User-Agent", configuration["UserAgent"])
                );

            builder.Services
                .AddHttpClient<IGravatarService, GravatarService>(client =>
                {
                    client.BaseAddress = new Uri(configuration["Urls:Gravatar.Api"]!);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("User-Agent", configuration["UserAgent"]);
                });

            builder.Services
                .AddScoped<IWebhookRetriever, WebhookRetriever>()
                .AddHttpClient<IWebhookSender, WebhookSender>()
                .AddStandardResilienceHandler();

            return builder.Services;
        }

        public IHostApplicationBuilder AddSerilog(string appName, Action<LoggerConfiguration> configureLogger)
        {
            builder.Services
                .AddSerilog((_, configuration) =>
                {
                    configuration
                        .Enrich.WithProperty("ApplicationName", appName)
                        .Enrich.FromLogContext()
                        .ReadFrom.Configuration(builder.Configuration);

                    if (builder.Environment.IsDevelopment())
                        configuration.WriteTo.Console(outputTemplate:
                            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}"
                        );
                    else
                        configuration.WriteTo.Console(new ElwarkJsonFormatter());

                    configureLogger(configuration);
                });

            return builder;
        }
    }
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

    public byte[] AppKey { get; init; }

    public byte[] AppVector { get; init; }
}

internal sealed class ElwarkJsonFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _valueFormatter;

    public ElwarkJsonFormatter(JsonValueFormatter? valueFormatter = null) =>
        _valueFormatter = valueFormatter ?? new JsonValueFormatter("$type");

    public void Format(LogEvent logEvent, TextWriter output)
    {
        FormatEvent(logEvent, output, _valueFormatter);
        output.WriteLine();
    }

    private static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(valueFormatter);

        output.Write('{');
        output.Write($"\"@t\":\"{logEvent.Timestamp.UtcDateTime:O}\",\"@l\":\"{logEvent.Level}\"");
        output.Write(",\"@m\":");

        var message = logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture);
        JsonValueFormatter.WriteQuotedJsonString(message, output);

        if (logEvent.Exception != null)
        {
            output.Write(",\"@x\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        foreach (var property in logEvent.Properties)
        {
            var name = property.Key;
            if (name.Length > 0 && name[0] == '@')
                name = '@' + name;

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        output.Write('}');
    }
}
