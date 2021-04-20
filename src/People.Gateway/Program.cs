using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using People.Host;
using Serilog;

namespace People.Gateway
{
    public class Program
    {
        private const string AppName = "People.Gateway";

        public static Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = HostExtensions.CreateConfiguration(environment, args);

            Log.Logger = HostExtensions.CreateLogger(configuration, environment, AppName);

            var host = CreateHost(configuration, args);

            return host.RunAsync();
        }

        private static IHost CreateHost(IConfiguration configuration, string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureWebHostDefaults(builder =>
                    builder
                        .UseKestrel()
                        .UseStartup<Startup>()
                )
                .UseSerilog()
                .Build();
    }
}
