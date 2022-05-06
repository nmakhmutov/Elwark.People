using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Sequences;

internal sealed class SequenceGenerator : ISequenceGenerator
{
    private const string SequenceName = nameof(SequenceName);

    private static readonly FilterDefinition<Sequence> Filter =
        Builders<Sequence>.Filter.Eq(x => x.Name, SequenceName);

    private static readonly UpdateDefinition<Sequence> Update =
        Builders<Sequence>.Update.Inc(x => x.Value, 1);

    private static readonly FindOneAndUpdateOptions<Sequence, long> Options =
        new()
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
            Projection = Builders<Sequence>.Projection.Expression(x => x.Value)
        };


    private readonly PeopleDbContext _dbContext;

    public SequenceGenerator(PeopleDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<AccountId> NextAccountIdAsync(CancellationToken ct)
    {
        var result = await _dbContext.Sequences
            .FindOneAndUpdateAsync(Filter, Update, Options, ct);

        return result;
    }

    public static IEnumerable<Sequence> InitValues()
    {
        yield return new Sequence(SequenceName);
    }
}
