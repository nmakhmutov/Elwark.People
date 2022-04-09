using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace People.Infrastructure.Blacklist;

internal sealed class BlacklistService : IBlacklistService
{
    private readonly InfrastructureDbContext _dbContext;

    public BlacklistService(InfrastructureDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<bool> IsPasswordForbidden(string password, CancellationToken ct)
    {
        var count = await _dbContext.Blacklist.CountDocumentsAsync(
            Builders<BlacklistItem>.Filter.And(
                Builders<BlacklistItem>.Filter.Eq(x => x.Type, ForbiddenType.Password),
                Builders<BlacklistItem>.Filter.Eq(x => x.Value, password)
            ),
            new CountOptions { Limit = 1 },
            ct
        );

        return count > 0;
    }

    public async Task<bool> IsEmailHostDenied(string host, CancellationToken ct)
    {
        var count = await _dbContext.Blacklist.CountDocumentsAsync(
            Builders<BlacklistItem>.Filter.And(
                Builders<BlacklistItem>.Filter.Eq(x => x.Type, ForbiddenType.EmailHost),
                Builders<BlacklistItem>.Filter.Eq(x => x.Value, host)
            ),
            new CountOptions { Limit = 1 },
            ct
        );

        return count > 0;
    }
}
