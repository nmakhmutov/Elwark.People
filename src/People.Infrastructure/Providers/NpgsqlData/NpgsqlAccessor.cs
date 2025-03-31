using Microsoft.Extensions.Logging;
using Npgsql;

namespace People.Infrastructure.Providers.NpgsqlData;

internal sealed class NpgsqlAccessor : INpgsqlAccessor, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlAccessor(string connection, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(connection, nameof(connection));

        _dataSource = new NpgsqlDataSourceBuilder(connection)
            .UseLoggerFactory(loggerFactory)
            .EnableDynamicJson()
            .Build();
    }

    public SqlBuilder Sql(string sql) =>
        new(_dataSource, sql);

    public ValueTask DisposeAsync() =>
        _dataSource.DisposeAsync();
}
