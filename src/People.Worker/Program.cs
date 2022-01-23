using System;
using System.IO;
using Common.Kafka;
using Common.Mongo;
using Confluent.Kafka;
using Integration.Event;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using People.Infrastructure;
using People.Worker.IntegrationEventHandlers;
using People.Worker.Services.Gravatar;
using People.Worker.Services.IpInformation;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

const string appName = "People.Worker";
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("application", appName)
    .WriteTo.Console(
        "json".Equals(configuration["Serilog:Formatter"])
            ? new CompactJsonFormatter()
            : new MessageTemplateTextFormatter(
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}")
    )
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

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
