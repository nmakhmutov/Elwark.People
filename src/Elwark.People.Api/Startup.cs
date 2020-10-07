using System;
using System.IdentityModel.Tokens.Jwt;
using CorrelationId;
using CorrelationId.DependencyInjection;
using CorrelationId.HttpClient;
using Elwark.EventBus.Logging.EF;
using Elwark.EventBus.RabbitMq;
using Elwark.Extensions.AspNet;
using Elwark.Extensions.AspNet.HttpClientAppName;
using Elwark.Extensions.AspNet.HttpClientLogging;
using Elwark.Extensions.AspNet.Localization;
using Elwark.Extensions.AspNet.Middlewares;
using Elwark.People.Api.Application.IntegrationEventHandlers;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Api.Extensions;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Infrastructure.Binders;
using Elwark.People.Api.Infrastructure.ContextFactory;
using Elwark.People.Api.Infrastructure.Filters;
using Elwark.People.Api.Infrastructure.Security;
using Elwark.People.Api.Infrastructure.Services.Facebook;
using Elwark.People.Api.Infrastructure.Services.Google;
using Elwark.People.Api.Infrastructure.Services.Identity;
using Elwark.People.Api.Infrastructure.Services.Microsoft;
using Elwark.People.Api.Settings;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Infrastructure;
using Elwark.People.Infrastructure.Cache;
using Elwark.People.Infrastructure.Confirmation;
using Elwark.People.Infrastructure.Repositories;
using Elwark.People.Shared;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.Storage.Client;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Elwark.People.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddPeopleControllers()
                .AddPeopleHealthChecks(Configuration)
                .AddPeopleStore(Configuration)
                .AddPeopleConfiguration(Configuration)
                .AddPeopleAuthentication(Configuration)
                .AddPeopleAuthorization()
                .AddPeopleModules(Configuration)
                .AddPeopleSecurity(Configuration)
                .AddEventBus(Configuration);
        }

        public void Configure(IApplicationBuilder app) =>
            app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                })
                .UseCors(builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                )
                .UseCorrelationId()
                .UseStaticFiles()
                .UseRouting()
                .UseElwarkLoggingRequest()
                .UseElwarkRequestLocalization(Configuration.GetSection("Localization").Get<ElwarkLocalizationOption>())
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(builder =>
                {
                    builder.MapControllers();

                    builder.MapHealthChecks("/hc/live", new HealthCheckOptions
                    {
                        Predicate = x => x.Name.Contains("self")
                    });

                    builder.MapHealthChecks("/hc", new HealthCheckOptions
                    {
                        ResultStatusCodes = ElwarkHealthCheckExtensions.ResultStatusCodes,
                        ResponseWriter = ElwarkHealthCheckExtensions.ResponseWriter
                    });
                });
    }

    internal static class StartupExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IOAuthIntegrationEventService, OAuthIntegrationEventService>()
                .AddScoped<IIntegrationEventLogService, IntegrationEventLogService>()
                .AddElwarkRabbitMq(configuration.GetSection("MessageQueue").Get<ElwarkRabbitConfiguration>())
                .AddEventHandler<IntegrationEventFailedIntegrationEvent, RetryEventHandler>()
                .AddEventHandler<MergeAccountInformationIntegrationEvent, AccountMergerIntegrationEventHandler>()
                .AddEventHandler<AccountBanExpiredIntegrationEvent, BanExpiredIntegrationEventHandler>();

            return services;
        }

        public static IServiceCollection AddPeopleControllers(this IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.AllowEmptyInputInBodyModelBinding = false;
                    options.Filters.Add<HttpGlobalExceptionFilter>();
                    options.ModelBinderProviders.Insert(2, new IdentityModelBinderProvider());
                })
                .ConfigureApiBehaviorOptions(options =>
                    options.InvalidModelStateResponseFactory =
                        ProblemDetailsExtensions.InvalidModelStateResponseFactory)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.NullValueHandling = ElwarkJsonSettings.Value.NullValueHandling;
                    options.SerializerSettings.Formatting = ElwarkJsonSettings.Value.Formatting;
                    options.SerializerSettings.ContractResolver = ElwarkJsonSettings.Value.ContractResolver;
                    options.SerializerSettings.Converters = ElwarkJsonSettings.Value.Converters;
                    options.SerializerSettings.DateFormatHandling = ElwarkJsonSettings.Value.DateFormatHandling;
                    options.SerializerSettings.DateTimeZoneHandling = ElwarkJsonSettings.Value.DateTimeZoneHandling;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddCorrelationId(options =>
                {
                    options.RequestHeader = "X-Correlation-Id";
                    options.ResponseHeader = "X-Correlation-Id";
                    options.AddToLoggingScope = true;
                    options.EnforceHeader = false;
                    options.IgnoreRequestHeader = false;
                    options.IncludeInResponse = true;
                    options.UpdateTraceIdentifier = true;
                })
                .WithTraceIdentifierProvider();

            return services;
        }

        public static IServiceCollection AddPeopleStore(this IServiceCollection services, IConfiguration configuration)
        {
            var postgres = configuration.GetConnectionString("postgres");
            var redis = configuration.GetConnectionString("redis");

            return services
                .AddDbContext<OAuthContext>(OAuthContextFactory.ContextOption(postgres))
                .AddDbContext<IntegrationEventLogContext>(IntegrationEventLogContextFactory.ContextOption(postgres))
                .AddNpgsqlQueryExecutor(postgres)
                .AddSingleton<IConnectionMultiplexer>(provider =>
                {
                    var options = ConfigurationOptions.Parse(redis, true);
                    options.ResolveDns = true;

                    return ConnectionMultiplexer.Connect(options);
                })
                .AddSingleton<ICacheStorage, CacheStorage>()
                .AddScoped<IConfirmationStore, ConfirmationStore>()
                .AddScoped<IAccountRepository, AccountRepository>();
        }

        public static IServiceCollection AddPeopleHealthChecks(this IServiceCollection services,
            IConfiguration configuration)
        {
            var postgres = configuration.GetConnectionString("postgres");
            var redis = configuration.GetConnectionString("redis");
            var rabbit = configuration.GetSection("MessageQueue")
                .Get<ElwarkRabbitConfiguration>()
                .GetConnectionString();

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddNpgSql(postgres)
                .AddRedis(redis)
                .AddRabbitMQ(rabbit, new SslOption());

            return services;
        }

        public static IServiceCollection AddPeopleConfiguration(this IServiceCollection services,
            IConfiguration configuration) =>
            services.AddOptions()
                .Configure<PasswordSettings>(configuration.GetSection("Password"))
                .Configure<ConfirmationSettings>(configuration.GetSection("Confirmation"));

        public static IServiceCollection AddPeopleAuthorization(this IServiceCollection services) =>
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policy.Common, Policy.CommonPolicy());

                options.AddPolicy(Policy.Identity, Policy.IdentityPolicy());

                options.AddPolicy(Policy.Account, Policy.AccountPolicy());
            });

        public static IServiceCollection AddPeopleAuthentication(this IServiceCollection services,
            IConfiguration configuration)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = configuration["Urls:OAuthIdentity"];
                    options.Audience = "elwark.people.api";
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true
                    };
                });

            return services;
        }

        public static IServiceCollection AddPeopleModules(this IServiceCollection services,
            IConfiguration configuration)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            services.AddMediatR(assemblies)
                .AddValidatorsFromAssemblies(assemblies);

            services.AddScoped<IIdentificationValidator, IdentificationValidator>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddTransient<IIdentityService, IdentityService>()
                .AddHttpClientLogging(options => options.IsLoggingResponse = false)
                .AddHttpClientAppNameHeader(options => options.AppName = Program.AppName);

            services.AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
                    client.BaseAddress = new Uri(configuration["Urls:GoogleApi"])
                )
                .AddLogging();

            services.AddHttpClient<IFacebookApiService, FacebookApiService>(client =>
                    client.BaseAddress = new Uri(configuration["Urls:FacebookApi"])
                )
                .AddLogging();

            services.AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
                    client.BaseAddress = new Uri(configuration["Urls:MicrosoftApi"])
                )
                .AddLogging();

            services.AddElwarkStorageClient(new Uri(configuration["Urls:StorageApi"]))
                .AddAppNameHeader()
                .AddLogging()
                .AddCorrelationIdForwarding();

            return services;
        }

        public static IServiceCollection AddPeopleSecurity(this IServiceCollection services,
            IConfiguration configuration)
        {
            var appSettings = configuration.GetSection("App").Get<AppSettings>();

            return services
                .AddScoped<IPasswordValidator, PasswordValidator>()
                .AddSingleton<IPasswordHasher>(provider => new PasswordHasher(appSettings.Key))
                .AddSingleton<IDataEncryption>(provider => new DataEncryption(appSettings.Key, appSettings.Iv));
        }
    }
}