using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace People.Host
{
    public static class HostExtensions
    {
        private const string LanguageParameterName = "Language";

        private static readonly CultureInfo DefaultCulture = new("en");

        private static IList<CultureInfo> SupportedCultures =>
            new List<CultureInfo>
            {
                new("en"),
                new("ru")
            };

        public static IApplicationBuilder UsePeopleLocalization(this IApplicationBuilder builder) =>
            builder.UseRequestLocalization(x =>
            {
                x.DefaultRequestCulture = new RequestCulture(DefaultCulture, DefaultCulture);
                x.SupportedCultures = SupportedCultures;
                x.SupportedUICultures = SupportedCultures;
                x.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new HeaderRequestCultureProvider(SupportedCultures, LanguageParameterName),
                    new QueryStringRequestCultureProvider
                    {
                        QueryStringKey = LanguageParameterName,
                        UIQueryStringKey = LanguageParameterName
                    }
                };
            });

        public static IConfiguration CreateConfiguration(string environment, string[] args) =>
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

        public static ILogger CreateLogger(IConfiguration configuration, string env, string app)
        {
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("application", app)
                .ReadFrom.Configuration(configuration);

            if ("Development".Equals(env, StringComparison.InvariantCultureIgnoreCase))
                logger.WriteTo.Console();
            else
                logger.WriteTo.Console(new ElwarkSerilogFormatter());

            return logger
                .CreateLogger();
        }
    }
}
