namespace People.Gateway.Features.Country.Models;

internal sealed record CountrySummary(string Alpha2Code, string Alpha3Code, string? Capital, string Name);
