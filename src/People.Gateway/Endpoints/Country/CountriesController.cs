using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Endpoints.Country.Model;
using People.Gateway.Endpoints.Country.Request;
using People.Gateway.Features.Country.Models;
using People.Gateway.Infrastructure;
using People.Grpc.Gateway;
using People.Grpc.Infrastructure;

namespace People.Gateway.Endpoints.Country;

[ApiController, Route("countries")]
public sealed class CountriesController : ControllerBase
{
    private readonly PeopleService.PeopleServiceClient _client;
    private readonly InfrastructureService.InfrastructureServiceClient _infrastructure;

    public CountriesController(PeopleService.PeopleServiceClient client,
        InfrastructureService.InfrastructureServiceClient infrastructure)
    {
        _client = client;
        _infrastructure = infrastructure;
    }

    [HttpGet("all"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var countries = await _client.GetCountriesAsync(
            new CountriesRequest { Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName },
            cancellationToken: ct
        );

        return Ok(countries.Countries.Select(x => new CountryCodeName(x.Code, x.Name)));
    }

    [HttpGet, Authorize(Policy = Policy.ManagementAccess)]
    public async Task<IActionResult> GetAsync([FromQuery] GetCountriesRequest request, CancellationToken ct)
    {
        var countries = await _infrastructure.GetCountriesAsync(
            new CountriesManagementRequest
            {
                Limit = request.Limit,
                Page = request.Page,
                CountryCode = request.Code
            },
            cancellationToken: ct
        );

        return Ok(
            new PageResponse<CountrySummary>(
                countries.Countries.Select(x => new CountrySummary(x.Alpha2Code, x.Alpha3Code, x.Capital, x.Name)),
                countries.Pages,
                countries.Count
            )
        );
    }

    [HttpGet("{code}"), Authorize(Policy = Policy.ManagementAccess)]
    public async Task<IActionResult> GetAsync(string code, CancellationToken ct)
    {
        var country = await _infrastructure.GetCountryAsync(new StringValue { Value = code }, cancellationToken: ct);

        return Ok(ToCountry(country));
    }

    [HttpPost, Authorize(Policy = Policy.ManagementAccess)]
    public async Task<IActionResult> CreateAsync([FromBody] Model.Country request, CancellationToken ct)
    {
        var country = await _infrastructure.CreateCountryAsync(
            new People.Grpc.Infrastructure.Country
            {
                Alpha2Code = request.Alpha2Code,
                Alpha3Code = request.Alpha3Code,
                Capital = request.Capital,
                Languages = { request.Languages },
                Region = request.Region,
                Subregion = request.Subregion,
                Translations = { request.Translations }
            },
            cancellationToken: ct
        );

        return Ok(ToCountry(country));
    }

    [HttpPut("{code}"), Authorize(Policy = Policy.ManagementAccess)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string code, [FromBody] Model.Country request,
        CancellationToken ct)
    {
        var country = await _infrastructure.UpdateCountryAsync(
            new People.Grpc.Infrastructure.Country
            {
                Alpha2Code = code,
                Alpha3Code = request.Alpha3Code,
                Capital = request.Capital,
                Languages = { request.Languages },
                Region = request.Region,
                Subregion = request.Subregion,
                Translations = { request.Translations }
            },
            cancellationToken: ct
        );

        return Ok(ToCountry(country));
    }

    [HttpDelete("{code}"), Authorize(Policy = Policy.ManagementAccess)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string code, CancellationToken ct)
    {
        await _infrastructure.DeleteCountryAsync(new StringValue { Value = code }, cancellationToken: ct);
        return NoContent();
    }

    private static Model.Country ToCountry(People.Grpc.Infrastructure.Country country) =>
        new(
            country.Alpha2Code,
            country.Alpha3Code,
            country.Capital,
            country.Region,
            country.Subregion,
            country.Languages,
            country.Translations
        );
}
