using People.Domain.ValueObjects;

namespace People.Api.Infrastructure.Providers.World;

internal sealed record CountryDetails(string Numeric, string Alpha2, string Alpha3, RegionCode Region);
