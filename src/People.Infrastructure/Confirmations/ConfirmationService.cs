using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Domain.AggregateModels.Account;

namespace People.Infrastructure.Confirmations
{
    public sealed class ConfirmationService : IConfirmationService
    {
        private readonly InfrastructureDbContext _dbContext;

        public ConfirmationService(InfrastructureDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task<int> CreateSignUpConfirmation(AccountId id, CancellationToken ct)
        {
            var code = GenerateCode();
            var confirmation = new Confirmation(id, ConfirmationType.SignUp, code, TimeSpan.FromMinutes(10));
            await _dbContext.Confirmations.InsertOneAsync(confirmation, new InsertOneOptions(), ct);

            return code;
        }

        public async Task<Confirmation?> GetSignUpConfirmation(AccountId id, CancellationToken ct)
        {
            var filter = Builders<Confirmation>.Filter.And(
                Builders<Confirmation>.Filter.Eq(x => x.AccountId, id),
                Builders<Confirmation>.Filter.Eq(x => x.Type, ConfirmationType.SignUp)
            );

            return await _dbContext.Confirmations
                .Find(filter)
                .Sort(Builders<Confirmation>.Sort.Descending(x => x.ExpireAt))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<int> CreateResetPasswordConfirmation(AccountId id, CancellationToken ct)
        {
            var code = GenerateCode();
            var confirmation = new Confirmation(id, ConfirmationType.ResetPassword, code, TimeSpan.FromMinutes(10));
            await _dbContext.Confirmations.InsertOneAsync(confirmation, new InsertOneOptions(), ct);

            return code;
        }

        public async Task<Confirmation?> GetResetPasswordConfirmation(AccountId id, CancellationToken ct)
        {
            var filter = Builders<Confirmation>.Filter.And(
                Builders<Confirmation>.Filter.Eq(x => x.AccountId, id),
                Builders<Confirmation>.Filter.Eq(x => x.Type, ConfirmationType.ResetPassword)
            );

            return await _dbContext.Confirmations
                .Find(filter)
                .Sort(Builders<Confirmation>.Sort.Descending(x => x.ExpireAt))
                .FirstOrDefaultAsync(ct);
        }

        private static int GenerateCode()
        {
            var random = new Random();
            return random.Next(1_000, 10_000);
        }
    }
}