using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elwark.People.Shared
{
    public static class DatabaseQueryExecutorExtensions
    {
        public static IServiceCollection AddNpgsqlQueryExecutor(this IServiceCollection services,
            string connectionString) => AddNpgsqlQueryExecutor(services, connectionString, 5, TimeSpan.FromSeconds(10));

        public static IServiceCollection AddNpgsqlQueryExecutor(this IServiceCollection services,
            string connectionString, uint retryCount, TimeSpan waitInterval) =>
            services.AddSingleton<IDatabaseQueryExecutor>(
                new NpgsqlQueryExecutor(connectionString, new DatabaseRetryPolicy(retryCount, waitInterval))
            );
    }
}