namespace People.Infrastructure.Providers.NpgsqlData;

public interface INpgsqlAccessor
{
    ISqlBuilder Sql(string sql);
}
