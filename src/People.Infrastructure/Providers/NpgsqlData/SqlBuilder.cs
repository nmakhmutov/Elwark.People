using Npgsql;

namespace People.Infrastructure.Providers.NpgsqlData;

public sealed class SqlBuilder
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly List<NpgsqlParameter> _parameters;
    private readonly string _sql;

    public SqlBuilder(NpgsqlDataSource dataSource, string sql)
    {
        _dataSource = dataSource;
        _sql = sql;
        _parameters = [];
    }

    public SqlBuilder AddParameter<T>(string parameterName, T? value) =>
        AddParameter(new NpgsqlParameter
        {
            ParameterName = parameterName,
            Value = value
        });

    private SqlBuilder AddParameter(NpgsqlParameter parameter)
    {
        _parameters.Add(parameter);
        return this;
    }

    public SqlReader<T> Select<T>(Func<NpgsqlDataReader, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return new SqlReader<T>(_dataSource, _sql, mapper, _parameters);
    }

    public SqlReader<T> Aggregate<T>(Action<Dictionary<Guid, T>, NpgsqlDataReader> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return new SqlReader<T>(_dataSource, _sql, mapper, _parameters);
    }

    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        await using var command = _dataSource.CreateCommand(_sql);

        foreach (var parameter in _parameters)
            command.Parameters.Add(parameter);

        return await command.ExecuteNonQueryAsync(ct);
    }
}
