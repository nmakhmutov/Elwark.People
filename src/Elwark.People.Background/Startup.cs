using System;
using System.Net.Http.Headers;
using Elwark.EventBus.RabbitMq;
using Elwark.Extensions.AspNet;
using Elwark.People.Background.Background;
using Elwark.People.Background.EventHandlers;
using Elwark.People.Background.Services;
using Elwark.People.Background.Services.Gravatar;
using Elwark.People.Background.TemplateViewEngine;
using Elwark.People.Shared;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.Storage.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Elwark.People.Background
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions()
                .AddMemoryCache()
                .AddFluid();

            services.AddElwarkStorageClient(new Uri(Configuration["Urls:StorageApi"]));

            services.AddHttpClient<IIpInformationService, IpInformationService>(client =>
                client.BaseAddress = new Uri(Configuration["Urls:IpInformationApi"]));

            services.AddHttpClient<IGravatarService, GravatarService>(client =>
            {
                client.BaseAddress = new Uri(Configuration["Urls:GravatarApi"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.157 Safari/537.36");
            });

            var postgres = Configuration.GetConnectionString("postgres");
            services.AddNpgsqlQueryExecutor(postgres, 5, TimeSpan.FromSeconds(10));

            services.AddSingleton<IEmailSendService, EmailSendService>()
                .AddSingleton<ITemplateBuilderService, TemplateBuilderService>();

            var rabbit = Configuration.GetSection("MessageQueue").Get<ElwarkRabbitConfiguration>();
            services.AddElwarkRabbitMq(rabbit)
                .AddEventHandler<ConfirmationByUrlCreatedIntegrationEvent, ConfirmationCreatedIntegrationEventHandler>()
                .AddEventHandler<ConfirmationByCodeCreatedIntegrationEvent, ConfirmationCreatedIntegrationEventHandler>()
                .AddEventHandler<AccountBanCreatedIntegrationEvent, AccountBanCreatedIntegrationEventHandler>()
                .AddEventHandler<AccountBanRemovedIntegrationEvent, AccountBanRemovedIntegrationEventHandler>()
                .AddEventHandler<AccountRegisteredIntegrationEvent, AccountRegisteredIntegrationEventHandler>()
                .AddEventHandler<AccountCreatedIntegrationEvent, GravatarSearcherAccountCreatedHandler>();

            services
                .AddHostedService<IntegrationEventLogsExpiredHostedService>()
                .AddHostedService<AccountBansExpiredHostedService>()
                .AddHostedService<ResendFailedEventHostedService>();

            services.AddRouting();
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddNpgSql(postgres)
                .AddUrlGroup(options => options.AddUri(new Uri(Configuration["Urls:OAuthApi"] + "/hc/live")), "api")
                .AddRabbitMQ(rabbit.GetConnectionString(), new SslOption());
        }

        public void Configure(IApplicationBuilder app) =>
            app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                })
                .UseRouting()
                .UseEndpoints(builder =>
                {
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
}