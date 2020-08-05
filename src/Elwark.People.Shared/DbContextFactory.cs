using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Elwark.People.Shared
{
    public abstract class DbContextFactory
    {
        private const string Table = "ef_migration";

        protected static string GetConnectionString() =>
            new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.Development.json")
                .Build()
                .GetConnectionString("postgres");

        protected static Action<DbContextOptionsBuilder> ContextOption(string connection, Assembly assembly) =>
            builder => builder.UseNpgsql(connection, option =>
                {
                    option.MigrationsAssembly(assembly.GetName().Name);
                    option.MigrationsHistoryTable(Table);
                    option.EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), Array.Empty<string>());
                })
                .EnableSensitiveDataLogging()
                .ConfigureWarnings(warnings =>
                    warnings.Throw(RelationalEventId.QueryPossibleExceptionWithAggregateOperatorWarning));
    }
}