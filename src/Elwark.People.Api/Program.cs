using System;
using System.IO;
using System.Threading.Tasks;
using Elwark.EventBus.Logging.EF;
using Elwark.Extensions.AspNet;
using Elwark.People.Infrastructure;
using Elwark.People.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Elwark.People.Api
{
    public static class Program
    {
        public const string AppName = "Elwark.People.Api";

        public static Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = CreateConfiguration(environment, args);

            Log.Logger = CreateLogger(configuration, environment, AppName);

            var host = CreateHost(args);

            if (configuration.GetValue("IsMigrate", false))
                host.MigrateDbContext<OAuthContext>()
                    .MigrateDbContext<IntegrationEventLogContext>();

            return host.RunAsync();
        }

        private static IHost CreateHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureWebHostDefaults(builder =>
                    builder.UseKestrel()
                        .UseStartup<Startup>()
                )
                .UseSerilog()
                .Build();

        private static IConfiguration CreateConfiguration(string environment, string[] args) =>
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

        private static ILogger CreateLogger(IConfiguration configuration, string environment, string app)
        {
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("application", app);

            if (environment == "Development")
                logger.WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss.fff} {Application} {Level:u3}] {RequestId} {SourceContext:lj} {Message:lj}{NewLine}{Exception}"
                );
            else
                logger.WriteTo.Console(new ElwarkCompactJsonFormatter());

            return logger
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }
    }
}