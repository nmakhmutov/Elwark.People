using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using People.Host;
using People.Infrastructure;
using Serilog;

namespace People.Notification
{
    public class Program
    {
        public const string AppName = "People.Notification";

        public static async Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = HostExtensions.CreateConfiguration(environment, args);

            Log.Logger = HostExtensions.CreateLogger(configuration, environment, AppName);

            var host = CreateHost(configuration, args);

            using (var scope = host.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<NotificationDbContext>()
                    .OnModelCreatingAsync();
            }

            await host.RunAsync();
        }

        private static IHost CreateHost(IConfiguration configuration, string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
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
    }
}