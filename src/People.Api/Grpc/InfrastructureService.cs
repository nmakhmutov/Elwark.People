using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using People.Grpc.Infrastructure;
using People.Infrastructure.Countries;
using Country = People.Grpc.Infrastructure.Country;

namespace People.Api.Grpc;

internal sealed class InfrastructureService : People.Grpc.Infrastructure.InfrastructureService.InfrastructureServiceBase
{
    private readonly ICountryService _country;

    public InfrastructureService(ICountryService country) =>
        _country = country;

    public override async Task<CountiesManagementReply> GetCountries(CountriesManagementRequest request,
        ServerCallContext context)
    {
        var (countries, pages, count) =
            await _country.GetAsync(request.CountryCode, request.Page, request.Limit, context.CancellationToken);

        return new CountiesManagementReply
        {
            Count = count,
            Pages = pages,
            Countries =
            {
                countries.Select(x => new CountiesManagementReply.Types.Country
                {
                    Capital = x.Capital,
                    Name = x.Name,
                    Alpha2Code = x.Alpha2Code,
                    Alpha3Code = x.Alpha3Code
                })
            }
        };
    }

    public override async Task<Country> GetCountry(StringValue request, ServerCallContext context)
    {
        var country = await _country.GetAsync(request.Value, context.CancellationToken);

        if (country is not null)
            return ToGrpc(country);

        context.Status = new Status(StatusCode.NotFound, $"Country {request.Value} not found");
        return new Country();
    }

    public override async Task<Country> CreateCountry(Country request, ServerCallContext context)
    {
        var country = await _country.CreateAsync(
            new People.Infrastructure.Countries.Country(
                request.Alpha2Code,
                request.Alpha3Code,
                request.Capital,
                request.Region,
                request.Subregion,
                request.Languages,
                request.Translations
            ),
            context.CancellationToken
        );

        return ToGrpc(country);
    }

    public override async Task<Country> UpdateCountry(Country request, ServerCallContext context)
    {
        var country = await _country.UpdateAsync(
            new People.Infrastructure.Countries.Country(
                request.Alpha2Code,
                request.Alpha3Code,
                request.Capital,
                request.Region,
                request.Subregion,
                request.Languages,
                request.Translations
            ),
            context.CancellationToken
        );

        return ToGrpc(country);
    }

    public override async Task<Empty> DeleteCountry(StringValue request, ServerCallContext context)
    {
        await _country.DeleteAsync(request.Value, context.CancellationToken);
        return new Empty();
    }

    private static Country ToGrpc(People.Infrastructure.Countries.Country country) =>
        new()
        {
            Capital = country.Capital,
            Languages = { country.Languages },
            Region = country.Region,
            Subregion = country.Subregion,
            Translations = { country.Translations },
            Alpha2Code = country.Alpha2Code,
            Alpha3Code = country.Alpha3Code
        };
}
