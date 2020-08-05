using System;
using Elwark.People.Infrastructure;
using Elwark.People.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elwark.People.Api.Infrastructure.ContextFactory
{
    public class OAuthContextFactory : DbContextFactory, IDesignTimeDbContextFactory<OAuthContext>
    {
        OAuthContext IDesignTimeDbContextFactory<OAuthContext>.CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OAuthContext>();
            ContextOption(GetConnectionString(), typeof(OAuthContext).Assembly)
                .Invoke(optionsBuilder);

            return new OAuthContext(optionsBuilder.Options);
        }

        public static Action<DbContextOptionsBuilder> ContextOption(string connection) =>
            ContextOption(connection, typeof(OAuthContext).Assembly);
    }
}