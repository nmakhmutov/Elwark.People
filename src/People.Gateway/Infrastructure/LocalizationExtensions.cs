using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;

namespace People.Gateway.Infrastructure
{
    internal static class LocalizationExtensions
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
    }
}
