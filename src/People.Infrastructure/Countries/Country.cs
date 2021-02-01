using System.Collections.Generic;
using MongoDB.Bson;

namespace People.Infrastructure.Countries
{
    public sealed record Country(
        ObjectId Id,
        string Alpha2Code,
        string Alpha3Code,
        string Capital,
        string Region,
        string Subregion,
        IEnumerable<string> Languages,
        IDictionary<string, string> Translations
    );
}