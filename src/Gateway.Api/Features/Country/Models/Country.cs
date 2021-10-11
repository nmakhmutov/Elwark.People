using System.Collections.Generic;

namespace Gateway.Api.Features.Country.Models;

public sealed record Country(
    string Alpha2Code,
    string Alpha3Code,
    string? Capital,
    string Region,
    string? Subregion,
    IEnumerable<string> Languages,
    IDictionary<string, string> Translations
);
