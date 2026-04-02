namespace People.Application.Providers.Postgres;

public interface ISqlBuilder
{
    ISqlBuilder AddParameter<T>(string parameterName, T? value);

    ISqlReader<TResult> Select<TResult>(Func<INpgsqlRow, TResult> mapper);

    ISqlReader<TResult> Aggregate<TResult>(Action<Dictionary<Guid, TResult>, INpgsqlRow> mapper);

    Task<int> ExecuteAsync(CancellationToken ct = default);
}
