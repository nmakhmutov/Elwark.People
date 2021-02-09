using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace People.Infrastructure.Timezones
{
    public sealed class TimezoneService : ITimezoneService
    {
        private readonly InfrastructureDbContext _dbContext;

        public TimezoneService(InfrastructureDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task<Timezone?> GetAsync(string name, CancellationToken ct) =>
            await _dbContext.Timezones
                .Find(Builders<Timezone>.Filter.Eq(x => x.Name, name))
                .FirstOrDefaultAsync(ct);
    }
}