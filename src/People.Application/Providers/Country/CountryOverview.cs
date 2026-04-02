using People.Domain.ValueObjects;

namespace People.Application.Providers.Country;

public sealed record CountryOverview(string Alpha2, string Alpha3, RegionCode Region, string Name);
