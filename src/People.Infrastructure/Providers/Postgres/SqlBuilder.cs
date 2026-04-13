using Npgsql;
using People.Application.Providers.Postgres;

namespace People.Infrastructure.Providers.Postgres;

public sealed class SqlBuilder : ISqlBuilder
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

    public ISqlBuilder AddParameter<T>(string parameterName, T? value) =>
        AddParameter(new NpgsqlParameter
        {
            ParameterName = parameterName,
            Value = value
        });

    public ISqlReader<TResult> Select<TResult>(Func<INpgsqlRow, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return new SqlReader<TResult>(_dataSource, _sql, mapper, _parameters);
    }

    public ISqlReader<TResult> Aggregate<TResult>(Action<Dictionary<Guid, TResult>, INpgsqlRow> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return new SqlReader<TResult>(_dataSource, _sql, mapper, _parameters);
    }

    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        await using var command = _dataSource.CreateCommand(_sql);

        foreach (var parameter in _parameters)
            command.Parameters.Add(parameter);

        return await command.ExecuteNonQueryAsync(ct);
    }

    private SqlBuilder AddParameter(NpgsqlParameter parameter)
    {
        _parameters.Add(parameter);
        return this;
    }
}
