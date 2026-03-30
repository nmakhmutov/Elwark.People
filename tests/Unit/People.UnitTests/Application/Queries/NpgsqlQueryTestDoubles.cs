using People.Infrastructure.Providers.NpgsqlData;

namespace People.UnitTests.Application.Queries;

internal sealed class DictionaryNpgsqlRow : INpgsqlRow
{
    private readonly IReadOnlyDictionary<int, object?> _values;

    public DictionaryNpgsqlRow(IReadOnlyDictionary<int, object?> values) =>
        _values = values;

    public bool IsDbNull(int ordinal) =>
        !_values.TryGetValue(ordinal, out var v) || v is null or DBNull;

    public long GetInt64(int ordinal) =>
        Convert.ToInt64(_values[ordinal]!);

    public string GetString(int ordinal) =>
        (string)_values[ordinal]!;

    public bool GetBoolean(int ordinal) =>
        (bool)_values[ordinal]!;

    public int GetInt32(int ordinal) =>
        Convert.ToInt32(_values[ordinal]!);

    public T GetFieldValue<T>(int ordinal) =>
        (T)_values[ordinal]!;
}

internal sealed class TestNpgsqlAccessor : INpgsqlAccessor
{
    private readonly IReadOnlyList<INpgsqlRow> _rows;

    public TestNpgsqlAccessor(params INpgsqlRow[] rows) =>
        _rows = rows;

    public ISqlBuilder Sql(string sql) =>
        new TestSqlBuilder(_rows);
}

internal sealed class TestSqlBuilder : ISqlBuilder
{
    private readonly IReadOnlyList<INpgsqlRow> _rows;

    public TestSqlBuilder(IReadOnlyList<INpgsqlRow> rows) =>
        _rows = rows;

    public ISqlBuilder AddParameter<T>(string parameterName, T? value) =>
        this;

    public ISqlReader<TResult> Select<TResult>(Func<INpgsqlRow, TResult> mapper) =>
        new TestSqlReader<TResult>(_rows, mapper);

    public ISqlReader<TResult> Aggregate<TResult>(Action<Dictionary<Guid, TResult>, INpgsqlRow> mapper) =>
        throw new NotSupportedException();

    public Task<int> ExecuteAsync(CancellationToken ct = default) =>
        Task.FromResult(0);
}

internal sealed class TestSqlReader<T> : ISqlReader<T>
{
    private readonly IReadOnlyList<INpgsqlRow> _rows;
    private readonly Func<INpgsqlRow, T> _mapper;

    public TestSqlReader(IReadOnlyList<INpgsqlRow> rows, Func<INpgsqlRow, T> mapper)
    {
        _rows = rows;
        _mapper = mapper;
    }

    public async IAsyncEnumerable<T> AsEnumerableAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var row in _rows)
        {
            await Task.Yield();
            yield return _mapper(row);
        }
    }

    public async Task<T?> FirstOrDefaultAsync(CancellationToken ct = default)
    {
        await foreach (var item in AsEnumerableAsync(ct))
            return item;

        return default;
    }

    public async Task<List<T>> ToListAsync(CancellationToken ct = default)
    {
        var list = new List<T>();
        await foreach (var item in AsEnumerableAsync(ct))
            list.Add(item);
        return list;
    }
}
