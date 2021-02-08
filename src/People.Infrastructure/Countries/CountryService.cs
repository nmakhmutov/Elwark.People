using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace People.Infrastructure.Countries
{
    public sealed class CountryService : ICountryService
    {
        private readonly InfrastructureDbContext _dbContext;

        public CountryService(InfrastructureDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task<Country?> GetAsync(string code, CancellationToken ct) =>
            code.Length switch
            {
                2 => await _dbContext.Countries.Find(Builders<Country>.Filter.Eq(x => x.Alpha2Code, code))
                    .FirstOrDefaultAsync(ct),

                3 => await _dbContext.Countries.Find(Builders<Country>.Filter.Eq(x => x.Alpha3Code, code))
                    .FirstOrDefaultAsync(ct),

                _ => null
            };
    }
}