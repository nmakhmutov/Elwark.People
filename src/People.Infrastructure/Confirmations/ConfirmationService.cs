using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Confirmations
{
    internal sealed class ConfirmationService : IConfirmationService
    {
        private readonly InfrastructureDbContext _dbContext;

        public ConfirmationService(InfrastructureDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task<Confirmation?> GetAsync(ObjectId id, CancellationToken ct)
        {
            var filter = Builders<Confirmation>.Filter.Eq(x => x.Id, id);

            return await _dbContext.Confirmations
                .Find(filter)
                .SortByDescending(x => x.ExpireAt)
                .FirstOrDefaultAsync(ct);
        }
        
        public async Task<Confirmation?> GetAsync(AccountId id, CancellationToken ct)
        {
            var filter = Builders<Confirmation>.Filter.Eq(x => x.AccountId, id);

            return await _dbContext.Confirmations
                .Find(filter)
                .SortByDescending(x => x.ExpireAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Confirmation> CreateAsync(AccountId id, TimeSpan lifetime, CancellationToken ct)
        {
            var random = new Random();
            var code = (uint) random.Next(1_000, 10_000);
            
            var confirmation = new Confirmation(id, code, DateTime.UtcNow.Add(lifetime));
            await _dbContext.Confirmations.InsertOneAsync(confirmation, new InsertOneOptions(), ct);

            return confirmation;
        }

        public Task DeleteAsync(AccountId id, CancellationToken ct) =>
            _dbContext.Confirmations.DeleteManyAsync(Builders<Confirmation>.Filter.Eq(x => x.AccountId, id), ct);
    }
}
