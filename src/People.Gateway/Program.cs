using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using People.Api.Infrastructure.Logger;
using Serilog;

namespace People.Gateway
{
    public class Program
    {
        public const string AppName = "Elwark.People.Gateway";

        public static Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = CreateConfiguration(environment, args);

            Log.Logger = CreateLogger(configuration, environment, AppName);

            var host = CreateHost(configuration, args);

            return host.RunAsync();
        }

        private static IHost CreateHost(IConfiguration configuration, string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureWebHostDefaults(builder =>
                    builder
                        .UseKestrel()
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

            if ("Development".Equals(environment, StringComparison.InvariantCultureIgnoreCase))
                logger.WriteTo.Console();
            else
                logger.WriteTo.Console(new ElwarkSerilogFormatter());

            return logger
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }
    }
}