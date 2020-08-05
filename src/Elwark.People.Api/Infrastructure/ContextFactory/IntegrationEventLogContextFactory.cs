using System;
using Elwark.EventBus.Logging.EF;
using Elwark.People.Infrastructure;
using Elwark.People.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elwark.People.Api.Infrastructure.ContextFactory
{
    public class IntegrationEventLogContextFactory : DbContextFactory,
        IDesignTimeDbContextFactory<IntegrationEventLogContext>
    {
        IntegrationEventLogContext IDesignTimeDbContextFactory<IntegrationEventLogContext>.CreateDbContext(
            string[] args)
        {
            var optionBuilder = new DbContextOptionsBuilder<IntegrationEventLogContext>();
            ContextOption(GetConnectionString(), typeof(OAuthContext).Assembly)
                .Invoke(optionBuilder);

            return new IntegrationEventLogContext(optionBuilder.Options);
        }

        public static Action<DbContextOptionsBuilder> ContextOption(string connection) =>
            ContextOption(connection, typeof(OAuthContext).Assembly);
    }
}