using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Mongo;
using MongoDB.Driver;
using People.Domain;

namespace People.Infrastructure.Countries;

internal sealed class CountryService : ICountryService
{
    private readonly InfrastructureDbContext _dbContext;

    public CountryService(InfrastructureDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<IReadOnlyCollection<CountrySummary>> GetAsync(Language language, CancellationToken ct) =>
        await _dbContext.Countries
            .Find(FilterDefinition<Country>.Empty)
            .SortBy(x => x.Translations[language.ToString()])
            .Project(
                x => new CountrySummary(x.Alpha2Code, x.Alpha3Code, x.Capital, x.Translations[language.ToString()]))
            .ToListAsync(ct);

    public async Task<MongoPagingResult<CountrySummary>> GetAsync(string? code, int page, int limit,
        CancellationToken ct)
    {
        var sort = Builders<Country>.Sort.Ascending(x => x.Alpha2Code);
        var filter = code switch
        {
            { Length: 2 } => Builders<Country>.Filter.Eq(x => x.Alpha2Code, code.ToUpperInvariant()),
            { Length: 3 } => Builders<Country>.Filter.Eq(x => x.Alpha3Code, code.ToUpperInvariant()),
            _ => Builders<Country>.Filter.Empty
        };

        return await _dbContext.Countries.PagingAsync(
            filter,
            sort,
            x => new CountrySummary(x.Alpha2Code, x.Alpha3Code, x.Capital, x.Translations["en"]),
            page,
            limit,
            ct
        );
    }

    public async Task<Country?> GetAsync(string code, CancellationToken ct) =>
        code.Length switch
        {
            2 => await _dbContext.Countries
                .Find(Builders<Country>.Filter.Eq(x => x.Alpha2Code, code.ToUpperInvariant()))
                .FirstOrDefaultAsync(ct),

            3 => await _dbContext.Countries
                .Find(Builders<Country>.Filter.Eq(x => x.Alpha3Code, code.ToUpperInvariant()))
                .FirstOrDefaultAsync(ct),

            _ => null
        };

    public async Task<Country> CreateAsync(Country country, CancellationToken ct)
    {
        await _dbContext.Countries.InsertOneAsync(country, new InsertOneOptions(), ct);
        return country;
    }

    public async Task<Country> UpdateAsync(Country country, CancellationToken ct)
    {
        var filter = Builders<Country>.Filter.Eq(x => x.Alpha2Code, country.Alpha2Code);
        var result = await _dbContext.Countries.ReplaceOneAsync(filter, country, new ReplaceOptions(), ct);

        if (result.ModifiedCount > 0)
            return country;

        throw new MongoUpdateException($"Country '{country.Alpha2Code}' not updated");
    }

    public Task DeleteAsync(string code, CancellationToken ct) =>
        _dbContext.Countries.DeleteOneAsync(Builders<Country>.Filter.Eq(x => x.Alpha2Code, code), ct);
}
