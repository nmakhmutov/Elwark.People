namespace Gateway.Api.Features.Country.Models;

internal sealed record CountrySummary(string Alpha2Code, string Alpha3Code, string? Capital, string Name);
