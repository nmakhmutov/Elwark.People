using System.Linq.Expressions;
using MongoDB.Driver;

namespace Common.Mongo;

public static class MongoCollectionExtensions
{
    private const string DataKey = "data";
    private const string CountKey = "count";

    public static Task<MongoPagingResult<TOutput>> PagingAsync<TInput, TOutput>(
        this IMongoCollection<TInput> collection, FilterDefinition<TInput> filter, SortDefinition<TInput> sort,
        ProjectionDefinition<TInput, TOutput> projection, int page, int limit, CancellationToken ct = default)
    {
        var pipeline = PipelineDefinition<TInput, TOutput>.Create(
            new IPipelineStageDefinition[]
            {
                PipelineStageDefinitionBuilder.Sort(sort),
                PipelineStageDefinitionBuilder.Skip<TInput>((page - 1) * limit),
                PipelineStageDefinitionBuilder.Limit<TInput>(limit),
                PipelineStageDefinitionBuilder.Project(projection)
            });

        return PagingAsync(collection, filter, AggregateFacet.Create(DataKey, pipeline), limit, ct);
    }

    public static Task<MongoPagingResult<TOutput>> PagingAsync<TInput, TOutput>(
        this IMongoCollection<TInput> collection, FilterDefinition<TInput> filter, SortDefinition<TInput> sort,
        Expression<Func<TInput, TOutput>> projection, int page, int limit, CancellationToken ct = default)
    {
        var pipeline = PipelineDefinition<TInput, TOutput>.Create(
            new IPipelineStageDefinition[]
            {
                PipelineStageDefinitionBuilder.Sort(sort),
                PipelineStageDefinitionBuilder.Skip<TInput>((page - 1) * limit),
                PipelineStageDefinitionBuilder.Limit<TInput>(limit),
                PipelineStageDefinitionBuilder.Project(projection)
            });

        return PagingAsync(collection, filter, AggregateFacet.Create(DataKey, pipeline), limit, ct);
    }

    public static Task<MongoPagingResult<TInput>> PagingAsync<TInput>(
        this IMongoCollection<TInput> collection, FilterDefinition<TInput> filter, SortDefinition<TInput> sort,
        int page, int limit, CancellationToken ct = default)
    {
        var pipeline = PipelineDefinition<TInput, TInput>.Create(new IPipelineStageDefinition[]
        {
            PipelineStageDefinitionBuilder.Sort(sort),
            PipelineStageDefinitionBuilder.Skip<TInput>((page - 1) * limit),
            PipelineStageDefinitionBuilder.Limit<TInput>(limit)
        });

        return PagingAsync(collection, filter, AggregateFacet.Create(DataKey, pipeline), limit, ct);
    }

    private static async Task<MongoPagingResult<TOutput>> PagingAsync<TInput, TOutput>(
        this IMongoCollection<TInput> collection, FilterDefinition<TInput> filter,
        AggregateFacet<TInput, TOutput> dataFacet, int limit, CancellationToken ct)
    {
        var countFacet = AggregateFacet.Create(CountKey,
            PipelineDefinition<TInput, AggregateCountResult>.Create(new[]
            {
                PipelineStageDefinitionBuilder.Count<TInput>()
            }));

        var aggregation = await collection.Aggregate()
            .Match(filter)
            .Facet(countFacet, dataFacet)
            .ToListAsync(ct);

        var facets = aggregation.First().Facets;

        var count = facets.First(x => x.Name == CountKey)
            .Output<AggregateCountResult>()
            .FirstOrDefault()
            ?.Count ?? 0;

        var pages = (uint)Math.Ceiling((double)count / limit);

        var data = facets.First(x => x.Name == DataKey)
            .Output<TOutput>();

        return new MongoPagingResult<TOutput>(data, pages, count);
    }
}

public sealed record MongoPagingResult<T>(IReadOnlyCollection<T> Items, uint Pages, long Count);
