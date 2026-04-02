using People.Domain.ValueObjects;

namespace People.Application.Providers.Country;

public sealed record CountryDetails(string Numeric, string Alpha2, string Alpha3, RegionCode Region);
