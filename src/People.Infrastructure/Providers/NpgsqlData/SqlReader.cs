using System.Runtime.CompilerServices;
using Npgsql;

namespace People.Infrastructure.Providers.NpgsqlData;

public sealed class SqlReader<T>
{
    private readonly string _connection;
    private readonly Func<NpgsqlDataReader, T> _mapper;
    private readonly IEnumerable<NpgsqlParameter> _parameters;
    private readonly string _sql;

    public SqlReader(
        string connection,
        string sql,
        IEnumerable<NpgsqlParameter> parameters,
        Func<NpgsqlDataReader, T> mapper
    )
    {
        _connection = connection;
        _sql = sql;
        _parameters = parameters;
        _mapper = mapper;
    }

    public async Task<T?> FirstOrDefaultAsync(CancellationToken ct = default)
    {
        await using var source = NpgsqlDataSource.Create(_connection);
        await using var command = source.CreateCommand(_sql);

        foreach (var parameter in _parameters)
            command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
            return _mapper(reader);

        return default;
    }

    public async IAsyncEnumerable<T> AsEnumerableAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var source = NpgsqlDataSource.Create(_connection);
        await using var command = source.CreateCommand(_sql);

        foreach (var parameter in _parameters)
            command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return _mapper(reader);
        }
    }

    public async Task<List<T>> ToListAsync(CancellationToken ct)
    {
        var result = new List<T>();

        await foreach (var item in AsEnumerableAsync(ct))
            result.Add(item);

        return result;
    }
}
