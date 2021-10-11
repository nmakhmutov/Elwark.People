namespace People.Infrastructure.Countries;

public sealed record CountrySummary(string Alpha2Code, string Alpha3Code, string? Capital, string Name);
