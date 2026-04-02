using Npgsql;
using People.Application.Providers.Postgres;

namespace People.Infrastructure.Providers.Postgres;

internal sealed class NpgsqlRowAdapter : INpgsqlRow
{
    private readonly NpgsqlDataReader _reader;

    public NpgsqlRowAdapter(NpgsqlDataReader reader) =>
        _reader = reader;

    public long GetInt64(int ordinal) =>
        _reader.GetInt64(ordinal);

    public string GetString(int ordinal) =>
        _reader.GetString(ordinal);

    public bool IsDbNull(int ordinal) =>
        _reader.IsDBNull(ordinal);

    public bool GetBoolean(int ordinal) =>
        _reader.GetBoolean(ordinal);

    public int GetInt32(int ordinal) =>
        _reader.GetInt32(ordinal);

    public T GetFieldValue<T>(int ordinal) =>
        _reader.GetFieldValue<T>(ordinal);
}
