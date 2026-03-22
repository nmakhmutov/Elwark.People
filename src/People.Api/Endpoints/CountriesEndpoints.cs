using System.Globalization;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Providers.World;

namespace People.Api.Endpoints;

internal static class CountriesEndpoints
{
    public static IEndpointRouteBuilder MapCountriesEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/countries")
            .WithTags("Countries")
            .RequireAuthorization(Policy.RequireCommonAccess.Name);

        group.MapGet("/", (ICountryClient client, CancellationToken ct) => client.GetAsync(CultureInfo.CurrentCulture, ct));

        return group;
    }
}
