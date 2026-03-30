using Microsoft.Extensions.Logging;
using Npgsql;

namespace People.Infrastructure.Providers.NpgsqlData;

internal sealed class NpgsqlAccessor : INpgsqlAccessor, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlAccessor(string connection, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(connection);

        _dataSource = new NpgsqlDataSourceBuilder(connection)
            .UseLoggerFactory(loggerFactory)
            .EnableDynamicJson()
            .Build();
    }

    public ISqlBuilder Sql(string sql) =>
        new SqlBuilder(_dataSource, sql);

    public ValueTask DisposeAsync() =>
        _dataSource.DisposeAsync();
}
