namespace People.Gateway.Endpoints.Country.Model;

internal sealed record CountrySummary(string Alpha2Code, string Alpha3Code, string? Capital, string Name);
