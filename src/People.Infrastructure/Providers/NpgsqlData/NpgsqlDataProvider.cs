namespace People.Infrastructure.Providers.NpgsqlData;

internal sealed class NpgsqlDataProvider : INpgsqlDataProvider
{
    private readonly string _connection;

    public NpgsqlDataProvider(string connection) =>
        _connection = connection is { Length: 0 }
            ? throw new ArgumentException("Value cannot be null or empty.", nameof(connection))
            : connection;

    public SqlBuilder Sql(string sql) =>
        new(_connection, sql);
}
