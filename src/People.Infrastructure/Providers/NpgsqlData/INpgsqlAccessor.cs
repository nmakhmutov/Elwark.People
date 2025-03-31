namespace People.Infrastructure.Providers.NpgsqlData;

public interface INpgsqlAccessor
{
    SqlBuilder Sql(string sql);
}
