using System;
using Microsoft.Extensions.DependencyInjection;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Infrastructure.Confirmations;
using People.Account.Infrastructure.Countries;
using People.Account.Infrastructure.Forbidden;
using People.Account.Infrastructure.IpAddress;
using People.Account.Infrastructure.Password;
using People.Account.Infrastructure.Repositories;
using People.Account.Infrastructure.Sequences;
using People.Mongo;

namespace People.Account.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string appKey,
            Action<MongoDbOptions> options)
        {
            services
                .Configure(options)
                .AddScoped<PeopleDbContext>()
                .AddScoped<InfrastructureDbContext>()
                .AddScoped<ICountryService, CountryService>()
                .AddScoped<IForbiddenService, ForbiddenService>()
                .AddScoped<ISequenceGenerator, SequenceGenerator>()
                .AddScoped<IAccountRepository, AccountRepository>()
                .AddSingleton<IPasswordHasher>(_ => new PasswordHasher(appKey))
                .AddSingleton<IIpAddressHasher>(_ => new IpAddressHasher(appKey))
                .AddScoped<IConfirmationService, ConfirmationService>();

            return services;
        }
    }
}
