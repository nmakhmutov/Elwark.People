using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace People.Account.Infrastructure.Forbidden
{
    internal sealed class ForbiddenService : IForbiddenService
    {
        private readonly InfrastructureDbContext _dbContext;

        public ForbiddenService(InfrastructureDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task<bool> IsPasswordForbidden(string password, CancellationToken ct)
        {
            var data = await _dbContext.ForbiddenItems.Find(
                    Builders<ForbiddenItem>.Filter.And(
                        Builders<ForbiddenItem>.Filter.Eq(x => x.Type, ForbiddenType.Password),
                        Builders<ForbiddenItem>.Filter.Eq(x => x.Value, password)
                    )
                )
                .FirstOrDefaultAsync(ct);

            return data is not null;
        }

        public async Task<bool> IsEmailHostDenied(string host, CancellationToken ct)
        {
            var data = await _dbContext.ForbiddenItems.Find(
                    Builders<ForbiddenItem>.Filter.And(
                        Builders<ForbiddenItem>.Filter.Eq(x => x.Type, ForbiddenType.EmailHost),
                        Builders<ForbiddenItem>.Filter.Eq(x => x.Value, host)
                    )
                )
                .FirstOrDefaultAsync(ct);

            return data is not null;
        }
    }
}
