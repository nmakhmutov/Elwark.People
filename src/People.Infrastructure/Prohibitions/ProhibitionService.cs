using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace People.Infrastructure.Prohibitions
{
    public class ProhibitionService : IProhibitionService
    {
        private readonly InfrastructureDbContext _dbContext;

        public ProhibitionService(InfrastructureDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task<bool> IsPasswordForbidden(string password, CancellationToken ct)
        {
            var data = await _dbContext.Prohibitions.Find(
                    Builders<Prohibition>.Filter.And(
                        Builders<Prohibition>.Filter.Eq(x => x.Type, ProhibitionType.Password),
                        Builders<Prohibition>.Filter.Eq(x => x.Value, password)
                    )
                )
                .FirstOrDefaultAsync(ct);

            return data is not null;
        }

        public async Task<bool> IsEmailHostDenied(string host, CancellationToken ct)
        {
            var data = await _dbContext.Prohibitions.Find(
                    Builders<Prohibition>.Filter.And(
                        Builders<Prohibition>.Filter.Eq(x => x.Type, ProhibitionType.EmailHost),
                        Builders<Prohibition>.Filter.Eq(x => x.Value, host)
                    )
                )
                .FirstOrDefaultAsync(ct);

            return data is not null;
        }
    }
}