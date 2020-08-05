using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Elwark.People.Shared
{
    public interface IDatabaseQueryExecutor
    {
        Task<T?> SingleAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            Func<DbDataReader, T> converter, CancellationToken cancellationToken = default) where T : class;

        public Task<T> SingleAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            Func<DbDataReader, T> converter, Func<T> empty, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<T>> MultiplyAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            Func<DbDataReader, T> converter, CancellationToken cancellationToken = default);

        Task<bool> ExecuteAsync(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            CancellationToken cancellationToken);
    }
}