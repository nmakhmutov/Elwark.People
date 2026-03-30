using System.Globalization;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Providers.World;

namespace People.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Local
public static class DictionariesEndpoints
{
    private static readonly TimezoneOverview[] TimeZones = TimeZoneInfo.GetSystemTimeZones()
        .Where(x => x.HasIanaId)
        .Select(x => new TimezoneOverview(x.Id, x.DisplayName, x.BaseUtcOffset))
        .ToArray();

    public static IEndpointRouteBuilder MapDictionariesEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/countries",
                (ICountryClient client, CancellationToken ct) => client.GetAsync(CultureInfo.CurrentCulture, ct))
            .WithTags("Dictionaries")
            .RequireAuthorization(Policy.RequireCommonAccess.Name);

        routes.MapGet("/timezones", () => TimeZones)
            .WithTags("Dictionaries")
            .RequireAuthorization(Policy.RequireCommonAccess.Name);

        return routes;
    }

    private sealed record TimezoneOverview(string Id, string Name, TimeSpan Offset);
}
