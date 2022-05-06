using System.Collections.Generic;

namespace People.Gateway.Endpoints.Country.Model;

public sealed record Country(
    string Alpha2Code,
    string Alpha3Code,
    string? Capital,
    string Region,
    string? Subregion,
    IEnumerable<string> Languages,
    IDictionary<string, string> Translations
);
