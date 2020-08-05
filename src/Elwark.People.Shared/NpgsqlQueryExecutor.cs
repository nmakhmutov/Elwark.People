using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Elwark.People.Shared
{
    internal class NpgsqlQueryExecutor : IDatabaseQueryExecutor
    {
        private readonly string _connectionString;
        private readonly IRetryPolicy _policy;

        public NpgsqlQueryExecutor(string connectionString, IRetryPolicy policy)
        {
            _connectionString = connectionString;
            _policy = policy;
        }

        public async Task<T?> SingleAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            Func<DbDataReader, T> converter, CancellationToken cancellationToken) where T : class
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var (parameterName, value) in parameters)
                command.Parameters.AddWithValue(parameterName, value);

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
                return converter(reader);

            return null;
        }

        public async Task<T> SingleAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            Func<DbDataReader, T> converter, Func<T> empty, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var (parameterName, value) in parameters)
                command.Parameters.AddWithValue(parameterName, value);

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
                return converter(reader);

            return empty();
        }

        public async Task<IReadOnlyCollection<T>> MultiplyAsync<T>(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            Func<DbDataReader, T> converter, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var (parameterName, value) in parameters)
                command.Parameters.AddWithValue(parameterName, value);

            await connection.OpenAsync(cancellationToken);
            var result = new List<T>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(converter(reader));

            return result;
        }

        public Task<bool> ExecuteAsync(string sql, IEnumerable<KeyValuePair<string, object>> parameters,
            CancellationToken cancellationToken) =>
            _policy.ExecuteAsync(async ct =>
                {
                    await using var connection = new NpgsqlConnection(_connectionString);
                    await using var command = new NpgsqlCommand(sql, connection);
                    foreach (var (parameterName, value) in parameters)
                        command.Parameters.AddWithValue(parameterName, value);

                    await connection.OpenAsync(ct);
                    var result = await command.ExecuteNonQueryAsync(ct);

                    return result >= 0;
                },
                cancellationToken);
    }
}