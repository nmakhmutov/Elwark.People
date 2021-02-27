using System;
using System.Net.Http.Headers;
using Confluent.Kafka;
using CorrelationId;
using CorrelationId.DependencyInjection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using People.Domain.AggregateModels.EmailProvider;
using People.Infrastructure;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using People.Infrastructure.Mongo;
using People.Infrastructure.Repositories;
using People.Notification.Application.Behaviors;
using People.Notification.Application.IntegrationEventHandler;
using People.Notification.Grpc;
using People.Notification.Infrastructure.Interceptors;
using People.Notification.Options;
using People.Notification.Services;

namespace People.Notification
{
    public class Startup
    {
        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions()
                .AddCorrelationId(options =>
                {
                    options.UpdateTraceIdentifier = true;
                    options.AddToLoggingScope = true;
                })
                .WithTraceIdentifierProvider();

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
                .AddProducer<EmailMessageCreatedIntegrationEvent>(config =>
                    config.Topic = IntegrationEvent.EmailMessages
                )
                .AddConsumer<EmailMessageCreatedIntegrationEvent, EmailMessageCreatedHandler>(
                    config => config.Topic = IntegrationEvent.EmailMessages
                )
                .AddConsumer<ProviderExpiredIntegrationEvent, UpdateExpiredProviderEventHandler>(config =>
                    config.Topic = IntegrationEvent.ExpiredProviders
                );

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            services.AddMediatR(assemblies)
                .AddValidatorsFromAssemblies(assemblies)
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

            services.Configure<DbContextSettings>(Configuration.GetSection("Mongodb"))
                .AddScoped<NotificationDbContext>()
                .AddScoped<IEmailProviderRepository, EmailProviderRepository>();

            services.Configure<GmailOptions>(Configuration.GetSection("Gmail"));

            services.AddHttpClient<ISendgridProvider, SendgridProvider>(client =>
            {
                client.BaseAddress = new Uri(Configuration["Sendgrid:Host"]);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Configuration["Sendgrid:Key"]);
            });

            services.AddGrpc(options => options.Interceptors.Add<GlobalErrorInterceptor>());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) =>
            app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                })
                .UseCorrelationId()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<NotificationService>();

                    endpoints.MapGet("/", context => context.Response
                        .WriteAsync("Communication with gRPC endpoints must be made through a gRPC client."));
                });
    }
}