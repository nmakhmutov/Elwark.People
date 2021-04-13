using System;
using System.Net.Http.Headers;
using Confluent.Kafka;
using CorrelationId;
using CorrelationId.DependencyInjection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using People.Api.Application.Behaviors;
using People.Api.Application.IntegrationEventHandlers;
using People.Api.Grpc;
using People.Api.Infrastructure.EmailBuilder;
using People.Api.Infrastructure.Interceptors;
using People.Api.Infrastructure.IpAddress;
using People.Api.Infrastructure.Password;
using People.Api.Infrastructure.Provider.Email;
using People.Api.Infrastructure.Provider.Email.Gmail;
using People.Api.Infrastructure.Provider.Email.SendGrid;
using People.Api.Infrastructure.Provider.Social.Google;
using People.Api.Infrastructure.Provider.Social.Microsoft;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.EmailProvider;
using People.Host;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Countries;
using People.Infrastructure.Forbidden;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using People.Infrastructure.Mongo;
using People.Infrastructure.Repositories;
using People.Infrastructure.Sequences;
using People.Infrastructure.Timezones;

namespace People.Api
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions()
                .AddCorrelationId(options =>
                {
                    options.UpdateTraceIdentifier = true;
                    options.AddToLoggingScope = true;
                })
                .WithTraceIdentifierProvider();

            services
                .Configure<DbContextSettings>(Configuration.GetSection("Mongodb"))
                .AddScoped<PeopleDbContext>()
                .AddScoped<InfrastructureDbContext>()
                .AddScoped<ICountryService, CountryService>()
                .AddScoped<ITimezoneService, TimezoneService>()
                .AddScoped<IForbiddenService, ForbiddenService>()
                .AddScoped<ISequenceGenerator, SequenceGenerator>()
                .AddScoped<IAccountRepository, AccountRepository>()
                .AddScoped<IEmailProviderRepository, EmailProviderRepository>();

            services
                .Configure<PasswordValidationOptions>(Configuration.GetSection("PasswordValidation"))
                .AddScoped<IPasswordValidator, PasswordValidator>()
                .AddSingleton<IPasswordHasher>(_ => new PasswordHasher(Configuration["App:Key"]))
                .AddSingleton<IIpAddressHasher>(_ => new IpAddressHasher(Configuration["App:Key"]));

            services.AddScoped<IConfirmationService, ConfirmationService>();

            services.AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
                client.BaseAddress = new Uri(Configuration["Urls:GoogleApi"])
            );

            services.AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
                client.BaseAddress = new Uri(Configuration["Urls:MicrosoftApi"])
            );

            services.AddKafkaMessageBus()
                .ConfigureProducers(config => config.BootstrapServers = Configuration["Kafka:Servers"])
                .ConfigureConsumers(config =>
                {
                    config.BootstrapServers = Configuration["Kafka:Servers"];

                    config.GroupId = Program.AppName;
                    config.AutoOffsetReset = AutoOffsetReset.Earliest;
                    config.EnableAutoCommit = false;
                    config.EnablePartitionEof = true;
                    config.AllowAutoCreateTopics = true;
                })
                .AddProducer<EmailMessageCreatedIntegrationEvent>(
                    config => config.Topic = IntegrationEvent.EmailMessages
                )
                .AddConsumer<EmailMessageCreatedIntegrationEvent, EmailMessageCreatedHandler>(config =>
                {
                    config.Topic = IntegrationEvent.EmailMessages;
                    config.Threads = 2;
                })
                .AddProducer<AccountCreatedIntegrationEvent>(config => config.Topic = IntegrationEvent.CreatedAccounts)
                .AddConsumer<AccountInfoReceivedIntegrationEvent, AccountInfoEventHandler>(config =>
                {
                    config.Topic = IntegrationEvent.CollectedInformation;
                    config.Threads = 2;
                })
                .AddConsumer<ProviderExpiredIntegrationEvent, UpdateExpiredProviderEventHandler>(config =>
                    config.Topic = IntegrationEvent.ExpiredProviders
                );

            services.Configure<GmailOptions>(Configuration.GetSection("Gmail"));
            services.AddTransient<IEmailSender, GmailProvider>();
            services.AddHttpClient<IEmailSender, SendgridProvider>(client =>
            {
                client.BaseAddress = new Uri(Configuration["Sendgrid:Host"]);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Configuration["Sendgrid:Key"]);
            });

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            services
                .AddEmailBuilder()
                .AddMediatR(assemblies)
                .AddValidatorsFromAssemblies(assemblies)
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

            services.AddGrpc(options => options.Interceptors.Add<GlobalErrorInterceptor>());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                })
                .UseCorrelationId()
                .UseRouting()
                .UsePeopleLocalization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<GatewayService>();
                    endpoints.MapGrpcService<IdentityService>();

                    endpoints.MapGet("/", context => context.Response
                        .WriteAsync("Communication with gRPC endpoints must be made through a gRPC client."));
                });
        }
    }
}
