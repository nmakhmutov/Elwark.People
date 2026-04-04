using System.Globalization;
using People.Api.Infrastructure;
using People.Application.Providers.Country;

namespace People.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Local
public static class DictionariesEndpoints
{
    private static readonly TimezoneOverview[] TimeZones = TimeZoneInfo.GetSystemTimeZones()
        .Where(x => x.HasIanaId)
        .OrderBy(x => x.BaseUtcOffset)
        .ThenBy(x => x.Id)
        .Select(x => new TimezoneOverview(
            x.Id,
            x.DisplayName,
            $"{(x.BaseUtcOffset >= TimeSpan.Zero ? "+" : "-")}{x.BaseUtcOffset:hh\\:mm}"
        ))
        .ToArray();

    public static IEndpointRouteBuilder MapDictionariesEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/countries",
                (ICountryClient client, CancellationToken ct) => client.GetAsync(CultureInfo.CurrentCulture, ct))
            .WithTags("Dictionaries")
            .RequireAuthorization(Policy.RequireRead.Name);

        routes.MapGet("/timezones", () => TimeZones)
            .WithTags("Dictionaries")
            .RequireAuthorization(Policy.RequireRead.Name);

        return routes;
    }

    private sealed record TimezoneOverview(string Id, string Name, string Offset);
}
