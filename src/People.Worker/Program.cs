using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using People.Worker.Services.EmailBuilder;
using People.Worker.Services.Gravatar;
using People.Worker.Services.IpInformation;

namespace People.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient<IIpInformationService, IpInformationService>(client =>
                        client.BaseAddress = new Uri(context.Configuration["Urls:IpInformationApi"]));

                    services.AddHttpClient<IGravatarService, GravatarService>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["Urls:GravatarApi"]);
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("User-Agent",
                            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.157 Safari/537.36");
                    });

                    services
                        //.AddSingleton<IEmailSendService, EmailSendService>()
                        .AddSingleton<ITemplateBuilderService, TemplateBuilderService>();
                    //services.AddHostedService<Worker>();
                });
    }
}