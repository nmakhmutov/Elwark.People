namespace People.Infrastructure.Providers.NpgsqlData;

public interface ISqlReader<TResult>
{
    IAsyncEnumerable<TResult> AsEnumerableAsync(CancellationToken ct = default);

    Task<TResult?> FirstOrDefaultAsync(CancellationToken ct = default);

    Task<List<TResult>> ToListAsync(CancellationToken ct = default);
}
