using Npgsql;

namespace People.Infrastructure.Providers.NpgsqlData;

public sealed class SqlBuilder
{
    private readonly string _connection;
    private readonly List<NpgsqlParameter> _parameters;
    private readonly string _sql;

    public SqlBuilder(string connection, string sql)
    {
        _connection = connection;
        _sql = sql;
        _parameters = new List<NpgsqlParameter>();
    }

    public SqlBuilder AddParameter(NpgsqlParameter parameter)
    {
        _parameters.Add(parameter);
        return this;
    }

    public SqlBuilder AddParameter(object value)
    {
        _parameters.Add(new NpgsqlParameter
        {
            Value = value
        });

        return this;
    }

    public SqlReader<T> Select<T>(Func<NpgsqlDataReader, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return new SqlReader<T>(_connection, _sql, _parameters, mapper);
    }

    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        await using var source = NpgsqlDataSource.Create(_connection);
        await using var command = source.CreateCommand(_sql);

        foreach (var parameter in _parameters)
            command.Parameters.Add(parameter);

        return await command.ExecuteNonQueryAsync(ct);
    }
}
