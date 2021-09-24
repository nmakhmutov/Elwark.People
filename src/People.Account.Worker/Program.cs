using System;
using System.IO;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using People.Host;
using People.Account.Infrastructure;
using People.Account.Worker.IntegrationEventHandlers;
using People.Account.Worker.Services.Gravatar;
using People.Account.Worker.Services.IpInformation;
using People.Integration.Event;
using People.Kafka;
using People.Mongo;
using Serilog;

const string appName = "People.Account.Worker";
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

Log.Logger = HostExtensions.CreateLogger(configuration, appName);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(builder => builder.AddConfiguration(configuration))
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

                config.GroupId = appName;
                config.AutoOffsetReset = AutoOffsetReset.Earliest;
                config.EnableAutoCommit = false;
                config.EnablePartitionEof = true;
                config.AllowAutoCreateTopics = true;
            })
            .AddProducer<AccountInfoReceivedIntegrationEvent>(
                config => config.Topic = IntegrationEvent.CollectedInformation
            )
            .AddConsumer<AccountCreatedIntegrationEvent, AccountCreatedIntegrationEventHandler>(config =>
            {
                config.Topic = IntegrationEvent.CreatedAccounts;
                config.Threads = 2;
            });

        // services.AddQuartz(options =>
        //     {
        //         options.UseMicrosoftDependencyInjectionJobFactory();
        //         options.AddJobAndTrigger<UpdateProviderJob>("0 */1 * ? * *");
        //     })
        //     .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services
            .Configure<MongoDbOptions>(context.Configuration.GetSection("Mongodb"))
            .AddTransient<PeopleDbContext>();
    })
    .UseSerilog()
    .Build();

await host.RunAsync();
