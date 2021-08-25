using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Sequences
{
    internal sealed class SequenceGenerator : ISequenceGenerator
    {
        private const string SequenceName = nameof(SequenceName);

        private static readonly FilterDefinition<Sequence> Filter =
            Builders<Sequence>.Filter.Eq(x => x.Name, SequenceName);

        private static readonly UpdateDefinition<Sequence>? Update = 
            Builders<Sequence>.Update.Inc(x => x.Value, 1);

        private readonly PeopleDbContext _dbContext;

        public SequenceGenerator(PeopleDbContext dbContext) =>
            _dbContext = dbContext;

        public async Task<AccountId> NextAccountIdAsync(CancellationToken ct)
        {
            var result = await _dbContext.Sequences
                .FindOneAndUpdateAsync(Filter, Update, new FindOneAndUpdateOptions<Sequence> {IsUpsert = false}, ct);

            return result.Value;
        }

        public static IEnumerable<Sequence> InitValues()
        {
            yield return new Sequence(SequenceName);
        }
    }
}
