using Npgsql;

namespace People.Infrastructure.Providers.NpgsqlData;

public sealed class SqlBuilder
{
    private readonly string _connection;
    private readonly IList<NpgsqlParameter> _parameters;
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
        _parameters.Add(new NpgsqlParameter { Value = value });
        return this;
    }

    public SqlReader<T> Select<T>(Func<NpgsqlDataReader, T> mapper) =>
        new(_connection, _sql, _parameters, mapper ?? throw new ArgumentNullException(nameof(mapper)));
    
    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connection);
        await using var command = new NpgsqlCommand(_sql, connection);
        foreach (var parameter in _parameters)
            command.Parameters.Add(parameter);

        await connection.OpenAsync(ct);

        return await command.ExecuteNonQueryAsync(ct);
    }
}
