using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Api.Infrastructure;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Grpc.Gateway;

namespace Gateway.Api.Features.Country;

[ApiController, Route("countries"), Authorize(Policy = Policy.RequireProfileAccess)]
public class CountriesController : ControllerBase
{
    private readonly PeopleService.PeopleServiceClient _client;

    public CountriesController(PeopleService.PeopleServiceClient client) =>
        _client = client;

    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var callOptions = new CallOptions(cancellationToken: ct);
        var countries = await _client.GetCountriesAsync(
            new CountriesRequest { Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName },
            callOptions
        );

        return Ok(countries.Countries.Select(x => new Country(x.Code, x.Name)));
    }
}
