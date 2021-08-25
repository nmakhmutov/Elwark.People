using System;
using Microsoft.Extensions.DependencyInjection;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.EmailProviderAggregate;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Countries;
using People.Infrastructure.Forbidden;
using People.Infrastructure.IpAddress;
using People.Infrastructure.Mongo;
using People.Infrastructure.Password;
using People.Infrastructure.Repositories;
using People.Infrastructure.Sequences;
using People.Infrastructure.Timezones;

namespace People.Infrastructure
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
                .AddScoped<ITimezoneService, TimezoneService>()
                .AddScoped<IForbiddenService, ForbiddenService>()
                .AddScoped<ISequenceGenerator, SequenceGenerator>()
                .AddScoped<IAccountRepository, AccountRepository>()
                .AddScoped<IEmailProviderRepository, EmailProviderRepository>()
                .AddSingleton<IPasswordHasher>(_ => new PasswordHasher(appKey))
                .AddSingleton<IIpAddressHasher>(_ => new IpAddressHasher(appKey))
                .AddScoped<IConfirmationService, ConfirmationService>();

            return services;
        }
    }
}
