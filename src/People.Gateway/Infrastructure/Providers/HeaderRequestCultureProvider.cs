using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace People.Gateway.Infrastructure.Providers;

internal sealed class HeaderRequestCultureProvider : IRequestCultureProvider
{
    private readonly IReadOnlyDictionary<string, ProviderCultureResult> _cultures;
    private readonly string _headerName;

    public HeaderRequestCultureProvider(IEnumerable<CultureInfo> cultures, string headerName)
    {
        _headerName = headerName;
        var result = new Dictionary<string, ProviderCultureResult>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var culture in cultures)
        {
            var language = culture.TwoLetterISOLanguageName;
            result.Add(language, new ProviderCultureResult(language, language));
        }

        _cultures = result;
    }

    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext) =>
        httpContext.Request.Headers.TryGetValue(_headerName, out var headerValue)
            ? Task.FromResult(_cultures.TryGetValue(headerValue.ToString(), out var value) ? value : null)
            : Task.FromResult<ProviderCultureResult?>(null);
}
