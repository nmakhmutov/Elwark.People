using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using People.Api.Infrastructure.Logger;
using People.Infrastructure;
using Serilog;

namespace People.Api
{
    public class Program
    {
        public const string AppName = "Elwark.People.Api";

        public static async Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = CreateConfiguration(environment, args);

            Log.Logger = CreateLogger(configuration, environment, AppName);

            var host = CreateHost(configuration, args);

            using (var scope = host.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<PeopleDbContext>()
                    .OnModelCreatingAsync();

                await scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>()
                    .OnModelCreatingAsync();

                // await new InfrastructureContextSeed(
                //     scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>(),
                //     scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>()
                // ).SeedAsync();
            }

            await host.RunAsync();
        }

        private static IHost CreateHost(IConfiguration configuration, string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureWebHostDefaults(builder =>
                    builder.ConfigureKestrel(options =>
                            options.Listen(IPAddress.Any, int.Parse(configuration["Grpc:Port"]), x =>
                                x.Protocols = HttpProtocols.Http2
                            )
                        )
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