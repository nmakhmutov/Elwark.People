using System.Runtime.CompilerServices;
using Npgsql;

namespace People.Infrastructure.Providers.NpgsqlData;

public sealed class SqlReader<T>
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly NpgsqlParameter[] _parameters;
    private readonly Func<NpgsqlDataReader, IAsyncEnumerable<T>> _processor;
    private readonly string _sql;

    public SqlReader(
        NpgsqlDataSource dataSource,
        string sql,
        Func<NpgsqlDataReader, T> mapper,
        IEnumerable<NpgsqlParameter> parameters
    )
    {
        _dataSource = dataSource;
        _sql = sql;
        _parameters = parameters.ToArray();
        _processor = Processor;

        return;

        async IAsyncEnumerable<T> Processor(NpgsqlDataReader reader)
        {
            while (await reader.ReadAsync())
                yield return mapper(reader);
        }
    }

    public SqlReader(
        NpgsqlDataSource dataSource,
        string sql,
        Action<Dictionary<Guid, T>, NpgsqlDataReader> mapper,
        IEnumerable<NpgsqlParameter> parameters
    )
    {
        _dataSource = dataSource;
        _sql = sql;
        _parameters = parameters.ToArray();
        _processor = Processor;

        return;

        async IAsyncEnumerable<T> Processor(NpgsqlDataReader reader)
        {
            var state = new Dictionary<Guid, T>();

            while (await reader.ReadAsync())
                mapper(state, reader);

            foreach (var result in state.Values)
                yield return result;
        }
    }

    public async IAsyncEnumerable<T> AsEnumerableAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var command = _dataSource.CreateCommand(_sql);

        foreach (var parameter in _parameters)
            command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(ct);

        await foreach (var result in _processor(reader).WithCancellation(ct))
            yield return result;
    }

    public async Task<T?> FirstOrDefaultAsync(CancellationToken ct = default)
    {
        await foreach (var item in AsEnumerableAsync(ct))
            return item;

        return default;
    }

    public async Task<List<T>> ToListAsync(CancellationToken ct = default)
    {
        var result = new List<T>();

        await foreach (var item in AsEnumerableAsync(ct))
            result.Add(item);

        return result;
    }
}
