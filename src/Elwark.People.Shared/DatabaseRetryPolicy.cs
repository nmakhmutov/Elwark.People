using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Polly;
using Polly.Retry;

namespace Elwark.People.Shared
{
    internal class DatabaseRetryPolicy : IRetryPolicy
    {
        private readonly AsyncRetryPolicy _retryPolicyAsync;

        private readonly string[] _sqlExceptions =
        {
            ErrorCodes.ConnectionException,
            ErrorCodes.ConnectionDoesNotExist,
            ErrorCodes.ConnectionFailure,
            ErrorCodes.SqlClientUnableToEstablishSqlConnection,
            ErrorCodes.SqlServerRejectedEstablishmentOfSqlConnection,
            ErrorCodes.TransactionResolutionUnknown,
            ErrorCodes.ProtocolViolation,
            ErrorCodes.TooManyConnections,
            ErrorCodes.ConfigurationLimitExceeded
        };

        public DatabaseRetryPolicy(uint retryCount, TimeSpan waitBetweenRetries)
        {
            if (retryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount));

            if (waitBetweenRetries <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(waitBetweenRetries));

            _retryPolicyAsync = Policy
                .Handle<PostgresException>(exception => _sqlExceptions.Contains(exception.SqlState))
                .WaitAndRetryAsync((int) retryCount, attempt => waitBetweenRetries);
        }

        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken) =>
            _retryPolicyAsync.ExecuteAsync(action, cancellationToken);

        private static class ErrorCodes
        {
            public const string ConnectionException = "08000";
            public const string ConnectionDoesNotExist = "08003";
            public const string ConnectionFailure = "08006";
            public const string SqlClientUnableToEstablishSqlConnection = "08001";
            public const string SqlServerRejectedEstablishmentOfSqlConnection = "08004";
            public const string TransactionResolutionUnknown = "08007";
            public const string ProtocolViolation = "08P01";
            public const string TooManyConnections = "53300";
            public const string ConfigurationLimitExceeded = "53400";
        }
    }
}