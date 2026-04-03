using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using People.Api.Resources;
using People.Application.Providers;
using People.Application.Providers.Country;
using People.Application.Providers.Google;
using People.Application.Providers.Gravatar;
using People.Application.Providers.Ip;
using People.Application.Providers.Microsoft;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Infrastructure;

namespace People.IntegrationTests.Web;

public sealed class PeopleApiFactory : WebApplicationFactory<Errors>
{
    private readonly PostgreSqlFixture _postgres;
    private readonly IGoogleApiService _google = Substitute.For<IGoogleApiService>();
    private readonly IMicrosoftApiService _microsoft = Substitute.For<IMicrosoftApiService>();
    private readonly IGravatarService _gravatar = Substitute.For<IGravatarService>();
    private readonly IIpService _ipService = Substitute.For<IIpService>();
    private readonly ICountryClient _country = Substitute.For<ICountryClient>();

    public PeopleApiFactory(PostgreSqlFixture postgres) =>
        _postgres = postgres;

    /// <summary>Substitute for confirmation emails; tests may reconfigure with NSubstitute <c>Arg</c>.</summary>
    public INotificationSender Notification { get; } = Substitute.For<INotificationSender>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:Postgresql", _postgres.ConnectionString);
        builder.UseSetting("ConnectionStrings:Kafka", "127.0.0.1:19092");
        builder.UseSetting("Authentication:Authority", "https://people.api.tests");
        builder.UseSetting("Authentication:Audience", JwtTestTokens.Audience);
        builder.UseSetting("Notification:Host", "http://localhost");
        builder.UseSetting("Notification:ClientId", "test");
        builder.UseSetting("Notification:ClientSecret", "test");
        builder.UseSetting("Notification:Scope", "notification:send");
        builder.UseSetting("Urls:Google.Api", "http://localhost");
        builder.UseSetting("Urls:Microsoft.Api", "http://localhost");
        builder.UseSetting("Urls:Gravatar.Api", "http://localhost");
        builder.UseSetting("Urls:Countries.Api", "http://localhost");
        builder.UseSetting("UserAgent", "People.IntegrationTests");

        builder.ConfigureTestServices(services =>
        {
            RemoveKafkaHostedServices(services);

            RemoveAllOf<INotificationSender>(services);
            services.AddSingleton(Notification);

            RemoveAllOf<ICountryClient>(services);
            services.AddSingleton(_country);

            RemoveAllOf<IGoogleApiService>(services);
            services.AddSingleton(_google);

            RemoveAllOf<IMicrosoftApiService>(services);
            services.AddSingleton(_microsoft);

            RemoveAllOf<IGravatarService>(services);
            services.AddSingleton(_gravatar);

            RemoveAllOf<IIpService>(services);
            services.AddSingleton(_ipService);

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.MetadataAddress = null!;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = JwtTestTokens.Issuer,
                    ValidateAudience = true,
                    ValidAudience = JwtTestTokens.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTestTokens.SigningKey,
                    NameClaimType = "sub"
                };
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(long accountId, params string[] scopes)
    {
        var client = CreateClient();
        var token = scopes.Length == 0
            ? JwtTestTokens.CreateBearerWithoutScope(accountId)
            : JwtTestTokens.CreateBearer(accountId, scopes);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task ResetDatabaseAsync(CancellationToken ct = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db, ct);
    }

    private static void RemoveAllOf<T>(IServiceCollection services) where T : class
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(d);
    }

    private static void RemoveKafkaHostedServices(IServiceCollection services)
    {
        foreach (var d in services.ToList())
        {
            if (d.ServiceType != typeof(Microsoft.Extensions.Hosting.IHostedService))
                continue;

            if (IsKafkaConsumerHostedService(d))
                services.Remove(d);
        }
    }

    private static bool IsKafkaConsumerHostedService(ServiceDescriptor d)
    {
        if (d.ImplementationType is { } impl &&
            impl.FullName?.Contains("KafkaConsumer", StringComparison.Ordinal) == true)
            return true;

        if (d.ImplementationFactory is not null)
        {
            var returnType = d.ImplementationFactory.Method.ReturnType;
            if (returnType.FullName?.Contains("KafkaConsumer", StringComparison.Ordinal) == true)
                return true;
        }

        return false;
    }

    internal static async IAsyncEnumerable<CountryOverview> TestCountriesAsync()
    {
        yield return new CountryOverview("US", "USA", RegionCode.Parse("NA"), "United States");
        yield return new CountryOverview("AT", "AUT", RegionCode.Parse("EU"), "Austria");
        await Task.CompletedTask;
    }

    public void SetupDefaultIntegrationMocks()
    {
        _ipService.GetAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns((IpInformation?)null);

        _gravatar.GetAsync(Arg.Any<System.Net.Mail.MailAddress>())
            .Returns((GravatarProfile?)null);

        _country.GetAsync(Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_ => TestCountriesAsync());

        Notification.SendConfirmationAsync(
                Arg.Any<System.Net.Mail.MailAddress>(),
                Arg.Any<string>(),
                Arg.Any<Language>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }
}
