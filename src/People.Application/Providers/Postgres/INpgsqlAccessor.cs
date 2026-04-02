namespace People.Application.Providers.Postgres;

public interface INpgsqlAccessor
{
    ISqlBuilder Sql(string sql);
}
