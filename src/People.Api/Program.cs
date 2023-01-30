using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Fluid;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Notification.Grpc;
using People.Api.Application.Behaviour;
using People.Api.Application.IntegrationEvents.EventHandling;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Endpoints.Account;
using People.Api.Grpc;
using People.Api.Infrastructure;
using People.Api.Infrastructure.EmailBuilder;
using People.Api.Infrastructure.Interceptors;
using People.Api.Infrastructure.Notifications;
using People.Api.Infrastructure.Providers.Google;
using People.Api.Infrastructure.Providers.Gravatar;
using People.Api.Infrastructure.Providers.IpApi;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Infrastructure;
using People.Infrastructure.Kafka;
using Serilog;

const string appName = "People.Api";
const string mainCors = "MainCORS";
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCorrelationId(options => options.UpdateTraceIdentifier = true)
    .WithTraceIdentifierProvider();

builder.Services
    .AddCors(options =>
        options.AddPolicy(mainCors, policyBuilder => policyBuilder
            .WithOrigins(builder.Configuration.GetRequiredSection("Cors").Get<string[]>()!)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
            .AllowAnyHeader()
        ))
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
            new QueryStringRequestCultureProvider { QueryStringKey = "language", UIQueryStringKey = "language" },
            new AcceptLanguageHeaderRequestCultureProvider()
        };
    });

builder.Services
    .AddAuthorization(options =>
    {
        options.AddPolicy(Policy.RequireAuthenticatedUser.Name, Policy.RequireAuthenticatedUser.Policy);
        options.AddPolicy(Policy.RequireCommonAccess.Name, Policy.RequireCommonAccess.Policy);
        options.AddPolicy(Policy.RequireProfileAccess.Name, Policy.RequireProfileAccess.Policy);
        options.AddPolicy(Policy.RequireManagementAccess.Name, Policy.RequireManagementAccess.Policy);
    });

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Urls:Identity"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

var assemblies = AppDomain.CurrentDomain.GetAssemblies();

builder.Services
    .AddMediatR(assemblies)
    .AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped, null, true)
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>))
    .AddInfrastructure(options =>
    {
        options.PostgresqlConnectionString = builder.Configuration["Postgresql:ConnectionString"]!;
        options.RedisConnectionString = builder.Configuration["Redis:ConnectionString"]!;
        options.AppKey = builder.Configuration["App:Key"]!;
        options.AppVector = builder.Configuration["App:Vector"]!;
    })
    .AddKafkaMessageBus(appName, builder.Configuration["Kafka:Servers"]!)
    .AddProducer<AccountCreatedIntegrationEvent>(x => x.Topic = KafkaTopic.CreatedAccounts)
    .AddProducer<AccountUpdatedIntegrationEvent>(x => x.Topic = KafkaTopic.UpdatedAccounts)
    .AddProducer<AccountDeletedIntegrationEvent>(x => x.Topic = KafkaTopic.DeletedAccounts)
    .AddConsumer<AccountCreatedIntegrationEvent, AccountCreatedIntegrationEventHandler>(x =>
    {
        x.Topic = KafkaTopic.CreatedAccounts;
        x.Threads = 2;
    });

builder.Services
    .AddSingleton<INotificationSender, NotificationSender>()
    .AddSingleton<IEmailBuilder, EmailBuilder>()
    .AddFluid(options =>
    {
        options.ViewsFileProvider = builder.Environment.ContentRootFileProvider;
        options.TemplateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;
    });

builder.Services
    .AddGrpcClient<NotificationService.NotificationServiceClient>(options =>
    {
        options.Address = builder.Configuration.GetValue<Uri>("Urls:Notification.Api");
        options.ChannelOptionsActions.Add(channel =>
        {
            channel.Credentials = ChannelCredentials.Insecure;
            channel.ServiceConfig = new ServiceConfig
            {
                MethodConfigs =
                {
                    new MethodConfig
                    {
                        Names = { MethodName.Default },
                        RetryPolicy = new RetryPolicy
                        {
                            MaxAttempts = 5,
                            InitialBackoff = TimeSpan.FromSeconds(1),
                            MaxBackoff = TimeSpan.FromSeconds(3),
                            BackoffMultiplier = 1,
                            RetryableStatusCodes = { StatusCode.Unavailable }
                        }
                    }
                }
            };
            channel.HttpHandler = new HttpClientHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        });
    })
    .AddGrpcCorrelationIdForwarding();

builder.Services
    .AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Urls:Google.Api"]!)
    );

builder.Services
    .AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Urls:Microsoft.Api"]!)
    );

builder.Services
    .AddHttpClient<IIpApiService, IpApiService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Urls:Ip.Api"]!);
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
    .AddGrpc(options =>
    {
        options.UseCorrelationId();
        options.Interceptors.Add<GrpcExceptionInterceptor>();
    });

builder.Host
    .UseSerilog((context, configuration) => configuration
        .Enrich.WithProperty("ApplicationName", appName)
        .ReadFrom.Configuration(context.Configuration)
    );

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<PeopleDbContext>()
        .Database.MigrateAsync();

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 10
    })
    .UseCors(mainCors)
    .UseCorrelationId()
    .UseRequestLocalization()
    .UseAuthentication()
    .UseAuthorization()
    .UseGlobalExceptionHandler();

app.MapAccountEndpoints();

app.MapGrpcService<PeopleService>();

app.Run();
