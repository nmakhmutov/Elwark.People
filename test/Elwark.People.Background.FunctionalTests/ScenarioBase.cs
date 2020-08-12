using System.IO;
using System.Reflection;
using Elwark.People.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Elwark.People.Background.FunctionalTests
{
    public class ScenarioBase
    {
        protected static readonly AccountId Id = new AccountId(1);
        
        protected static readonly Notification.PrimaryEmail Email =
            new Notification.PrimaryEmail("makhmutov.nail@yahoo.com");
        
        public static TestServer CreateServer()
        {
            var path = Assembly.GetAssembly(typeof(ScenarioBase))?.Location;

            var hostBuilder = new WebHostBuilder()
                .UseContentRoot(Path.GetDirectoryName(path))
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.json", false)
                        .AddEnvironmentVariables();
                })
                .UseStartup<Startup>();

            var testServer = new TestServer(hostBuilder);

            return testServer;
        }
    }

}