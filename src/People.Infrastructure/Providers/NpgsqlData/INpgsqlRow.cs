namespace People.Infrastructure.Providers.NpgsqlData;

public interface INpgsqlRow
{
    long GetInt64(int ordinal);

    string GetString(int ordinal);

    bool IsDbNull(int ordinal);

    bool GetBoolean(int ordinal);

    int GetInt32(int ordinal);

    T GetFieldValue<T>(int ordinal);
}
