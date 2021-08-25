using System;
using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using People.Host;
using People.Infrastructure;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using People.Infrastructure.Mongo;
using People.Worker.IntegrationEventHandlers;
using People.Worker.Job;
using People.Worker.Services.Gravatar;
using People.Worker.Services.IpInformation;
using Quartz;
using Serilog;

namespace People.Worker
{
    public class Program
    {
        private const string AppName = "People.Worker";

        public static Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = HostExtensions.CreateConfiguration(environment, args);

            Log.Logger = HostExtensions.CreateLogger(configuration, AppName);

            var host = CreateHostBuilder(configuration, args);

            return host.RunAsync();
        }

        private static IHost CreateHostBuilder(IConfiguration configuration, string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient<IIpInformationService, IpInformationService>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["Urls:IpInformationApi"]);
                        client.DefaultRequestHeaders.Add("User-Agent", context.Configuration["UserAgent"]);
                    });

                    services.AddHttpClient<IGravatarService, GravatarService>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["Urls:GravatarApi"]);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("User-Agent", context.Configuration["UserAgent"]);
                    });

                    services.AddKafkaMessageBus()
                        .ConfigureProducers(config => config.BootstrapServers = context.Configuration["Kafka:Servers"])
                        .ConfigureConsumers(config =>
                        {
                            config.BootstrapServers = context.Configuration["Kafka:Servers"];

                            config.GroupId = AppName;
                            config.AutoOffsetReset = AutoOffsetReset.Earliest;
                            config.EnableAutoCommit = false;
                            config.EnablePartitionEof = true;
                            config.AllowAutoCreateTopics = true;
                        })
                        .AddProducer<AccountInfoReceivedIntegrationEvent>(
                            config => config.Topic = IntegrationEvent.CollectedInformation
                        )
                        .AddProducer<ProviderExpiredIntegrationEvent>(
                            config => config.Topic = IntegrationEvent.ExpiredProviders
                        )
                        .AddConsumer<AccountCreatedIntegrationEvent, AccountCreatedIntegrationEventHandler>(config =>
                        {
                            config.Topic = IntegrationEvent.CreatedAccounts;
                            config.Threads = 2;
                        });

                    services.AddQuartz(options =>
                        {
                            options.UseMicrosoftDependencyInjectionJobFactory();
                            options.AddJobAndTrigger<UpdateProviderJob>("0 */1 * ? * *");
                        })
                        .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

                    services
                        .Configure<MongoDbOptions>(context.Configuration.GetSection("Mongodb"))
                        .AddTransient<PeopleDbContext>();
                })
                .UseSerilog()
                .Build();
    }
}
