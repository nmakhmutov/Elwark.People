namespace People.Infrastructure.Providers.NpgsqlData;

public interface INpgsqlDataProvider
{
    SqlBuilder Sql(string sql);
}
