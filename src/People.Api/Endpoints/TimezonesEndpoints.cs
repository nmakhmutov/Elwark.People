using People.Api.Infrastructure;

// ReSharper disable NotAccessedPositionalProperty.Local

namespace People.Api.Endpoints;

internal static class TimezonesEndpoints
{
    private static readonly TimezoneOverview[] TimeZones = TimeZoneInfo.GetSystemTimeZones()
        .Where(x => x.HasIanaId)
        .Select(x => new TimezoneOverview(x.Id, x.DisplayName, x.BaseUtcOffset))
        .ToArray();

    public static IEndpointRouteBuilder MapTimezonesEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/timezones")
            .WithTags("Timezones")
            .RequireAuthorization(Policy.RequireCommonAccess.Name);

        group.MapGet("/", () => TimeZones);

        return group;
    }

    private sealed record TimezoneOverview(string Id, string Name, TimeSpan Offset);
}
