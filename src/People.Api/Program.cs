using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.AccessTokenManagement;
using FluentValidation;
using Fluid;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using People.Api.Application.Behaviour;
using People.Api.Application.IntegrationEvents.EventHandling;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Queries.GetAccountSummary;
using People.Api.Endpoints;
using People.Api.Endpoints.Account;
using People.Api.Grpc;
using People.Api.Infrastructure;
using People.Api.Infrastructure.EmailBuilder;
using People.Api.Infrastructure.Interceptors;
using People.Api.Infrastructure.Notifications;
using People.Api.Infrastructure.Providers;
using People.Api.Infrastructure.Providers.Google;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Api.Infrastructure.Providers.World;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Kafka;
using Scalar.AspNetCore;
using Scope = Duende.AccessTokenManagement.Scope;
using TimeZone = People.Domain.ValueObjects.TimeZone;

const string appName = "People.Api";
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration.GetUri("Authentication:Authority").AbsoluteUri;
        options.Audience = builder.Configuration.GetString("Authentication:Audience");
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromSeconds(10)
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(Policy.RequireAuthenticatedUser.Name, Policy.RequireAuthenticatedUser.Policy)
    .AddPolicy(Policy.RequireCommonAccess.Name, Policy.RequireCommonAccess.Policy)
    .AddPolicy(Policy.RequireProfileAccess.Name, Policy.RequireProfileAccess.Policy)
    .AddPolicy(Policy.RequireManagementAccess.Name, Policy.RequireManagementAccess.Policy);

builder.Services
    .AddOpenApi()
    .AddCors()
    .AddRequestLocalization(options =>
    {
        var cultures = new CultureInfo[]
        {
            new("en"),
            new("ru")
        };

        options.DefaultRequestCulture = new RequestCulture("en");
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;
        options.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new QueryStringRequestCultureProvider
            {
                QueryStringKey = "language",
                UIQueryStringKey = "language"
            },
            new AcceptLanguageHeaderRequestCultureProvider()
        };
    });

builder.Services
    .AddMediator(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Scoped;
        options.PipelineBehaviors = [typeof(RequestLoggingBehavior<,>), typeof(RequestValidatorBehavior<,>)];
    })
    .AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies(), ServiceLifetime.Scoped, null, true)
    .AddInfrastructure(options =>
    {
        var postgresql = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Postgresql"))
        {
            ApplicationName = appName
        };

        options.PostgresqlConnectionString = postgresql.ToString();
        options.AppKey = builder.Configuration["App:Key"]!;
        options.AppVector = builder.Configuration["App:Vector"]!;
    });

builder.Services
    .AddHybridCache();

builder.Services
    .Configure<HostOptions>(options =>
    {
        options.ServicesStartConcurrently = true;
        options.ServicesStopConcurrently = true;
    });

builder.Services
    .AddKafka(builder.Configuration.GetConnectionString("Kafka")!)
    .AddProducer<AccountCreatedIntegrationEvent>(producer =>
        producer.WithTopic(KafkaTopic.Created)
            .WithClientId(appName)
    )
    .AddProducer<AccountUpdatedIntegrationEvent>(producer =>
        producer.WithTopic(KafkaTopic.Updated)
            .WithClientId(appName)
    )
    .AddProducer<AccountDeletedIntegrationEvent>(producer =>
        producer.WithTopic(KafkaTopic.Deleted)
            .WithClientId(appName)
    )
    .AddProducer<AccountActivity>(producer =>
        producer.WithTopic(KafkaTopic.Activity)
            .WithClientId(appName)
    )
    .AddConsumer<AccountCreatedIntegrationEvent, AccountCreatedIntegrationEventHandler>(consumer =>
        consumer.WithTopic(KafkaTopic.Created)
            .WithGroupId(appName)
            .WithWorkers(2)
            .CreateTopicIfNotExists(8)
    )
    .AddConsumer<AccountActivity, AccountEngagedIntegrationEventHandler>(consumer =>
        consumer.WithTopic(KafkaTopic.Activity)
            .WithGroupId(appName)
            .WithWorkers(2)
            .CreateTopicIfNotExists(8)
    );

builder.Services
    .AddSingleton<IEmailBuilder, EmailBuilder>()
    .AddFluid(options =>
    {
        options.ViewsFileProvider = builder.Environment.ContentRootFileProvider;
        options.TemplateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;
    });

builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(ClientCredentialsClientName.Parse("notification"), client =>
    {
        client.TokenEndpoint = builder.Configuration.GetUri("Authentication:Authority", "connect/token");
        client.ClientId = ClientId.Parse(builder.Configuration.GetString("Notification:ClientId"));
        client.ClientSecret = ClientSecret.Parse(builder.Configuration.GetString("Notification:ClientSecret"));
        client.Scope = Scope.Parse(builder.Configuration.GetString("Notification:Scope"));
    });

builder.Services
    .AddHttpClient<INotificationSender, NotificationSender>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Notification:Host"]!)
    )
    .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("notification"));

builder.Services
    .AddHttpClient<ICountryClient, CountryClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Urls:Countries.Api"]!)
    );

builder.Services
    .AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Urls:Google.Api"]!)
    );

builder.Services
    .AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Urls:Microsoft.Api"]!)
    );

builder.Services
    .AddHttpClient<IIpService, IpApiService>(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", builder.Configuration["UserAgent"]);
    });

builder.Services
    .AddHttpClient<IIpService, GeoPluginService>(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", builder.Configuration["UserAgent"]);
    });

builder.Services
    .AddHttpClient<IIpService, IpQueryService>(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", builder.Configuration["UserAgent"]);
    });

builder.Services
    .AddHttpClient<IGravatarService, GravatarService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Urls:Gravatar.Api"]!);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", builder.Configuration["UserAgent"]);
    });

builder.Services
    .ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddProblemDetails();

builder.Services
    .AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    })
    .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
    .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services
    .AddGrpc(options => options.Interceptors.Add<GrpcExceptionInterceptor>());

builder.AddSerilog(appName, configuration => configuration
    .Destructure.AsScalar<AccountId>()
    .Destructure.AsScalar<Language>()
    .Destructure.AsScalar<RegionCode>()
    .Destructure.AsScalar<CountryCode>()
    .Destructure.AsScalar<TimeZone>()
    .Destructure.AsScalar<DateFormat>()
    .Destructure.AsScalar<TimeFormat>()
    .Destructure.AsScalar<IPAddress>()
    .Destructure.ByTransforming<Account>(x => new { x.Id, x.Name.Nickname })
    .Destructure.ByTransforming<AccountSummary>(x => new { x.Id, x.Name.Nickname })
);

await using var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 10
    })
    .UseCors(policy => policy
        .WithOrigins(builder.Configuration.GetRequiredSection("Cors").Get<string[]>()!)
        .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
        .AllowAnyHeader()
        .AllowCredentials()
    )
    .UseExceptionHandler()
    .UseRequestLocalization()
    .UseAuthentication()
    .UseAuthorization();

if (app.Environment.IsProduction())
    app.UseResponseCompression();

app.MapOpenApi();
app.MapScalarApiReference("/docs");

app.MapAccountEndpoints();
app.MapCountriesEndpoints();
app.MapTimezonesEndpoints();

app.MapGrpcService<PeopleService>();

await app.RunAsync();
