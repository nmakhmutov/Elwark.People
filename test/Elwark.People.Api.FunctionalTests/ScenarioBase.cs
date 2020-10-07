using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Infrastructure.Services.Facebook;
using Elwark.People.Api.Infrastructure.Services.Google;
using Elwark.People.Api.Infrastructure.Services.Microsoft;
using Elwark.People.Api.Infrastructure.Services.Identity;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.Storage.Client;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

namespace Elwark.People.Api.FunctionalTests
{
    public class ScenarioBase
    {
        public static TestServer CreateServer(IIdentityService? identityService = null)
        {
            var path = Assembly.GetAssembly(typeof(ScenarioBase))?.Location;

            var hostBuilder = new WebHostBuilder()
                .UseContentRoot(Path.GetDirectoryName(path))
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.json", false)
                        .AddEnvironmentVariables();
                })
                .ConfigureTestServices(services =>
                {
                    services.RemoveAll<IGoogleApiService>();
                    services.RemoveAll<IFacebookApiService>();
                    services.RemoveAll<IMicrosoftApiService>();
                    services.RemoveAll<IElwarkStorageClient>();
                    
                    services.AddHttpClient<IGoogleApiService, FakeApiService>();
                    services.AddHttpClient<IFacebookApiService, FakeApiService>();
                    services.AddHttpClient<IMicrosoftApiService, FakeApiService>();
                    services.AddSingleton<IElwarkStorageClient>(provider => new FakeStorage());
                    
                    if (identityService != null)
                    {
                        services.RemoveAll<IIdentityService>();
                        services.AddTransient(provider => identityService);
                    }
                    
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy(
                            Policy.Common,
                            builder => builder.RequireAssertion(context => true)
                        );

                        options.AddPolicy(
                            Policy.Identity,
                            builder => builder.RequireAssertion(context => true)
                        );

                        options.AddPolicy(
                            Policy.Account,
                            builder => builder.RequireAssertion(context => true)
                        );
                    });
                })
                .UseStartup<Startup>();

            var testServer = new TestServer(hostBuilder);

            return testServer;
        }
    }

    public class FakeIdentityService : IIdentityService
    {
        public Func<AccountId> AccountIdGenerator { get; set; } = () => new AccountId();
        
        public Func<IdentityId> IdentityIdGenerator { get; set; } = () => new IdentityId();
        
        public string GetSub() =>
            AccountIdGenerator().Value.ToString();

        public AccountId GetAccountId() =>
            AccountIdGenerator();

        public IdentityId GetIdentityId() =>
            IdentityIdGenerator();

        public string? GetIdentityName() =>
            throw new NotImplementedException();
    }

    public class FakeApiService : IGoogleApiService, IFacebookApiService, IMicrosoftApiService
    {
        public FakeApiService(HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri("http://localhost");
        }

        Task<GoogleAccount> IGoogleApiService.GetAsync(string accessToken, CancellationToken cancellationToken)
        {
            if (accessToken == "expired")
                throw new ElwarkGoogleException(GoogleError.TokenExpired);

            return Task.FromResult(JsonConvert.DeserializeObject<GoogleAccount>(accessToken));
        }

        Task<FacebookAccount> IFacebookApiService.GetAsync(string accessToken, CancellationToken cancellationToken)
        {
            if (accessToken == "expired")
                throw new ElwarkFacebookException(FacebookError.TokenExpired);

            return Task.FromResult(JsonConvert.DeserializeObject<FacebookAccount>(accessToken));
        }

        Task<MicrosoftAccount> IMicrosoftApiService.GetAsync(string accessToken, CancellationToken cancellationToken)
        {
            if (accessToken == "expired")
                throw new ElwarkMicrosoftException(MicrosoftError.TokenExpired);

            return Task.FromResult(JsonConvert.DeserializeObject<MicrosoftAccount>(accessToken));
        }
    }
}