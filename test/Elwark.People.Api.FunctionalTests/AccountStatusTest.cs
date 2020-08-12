using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Infrastructure;
using Elwark.People.Shared.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elwark.People.Api.FunctionalTests
{
    public class AccountStatusTest : ScenarioBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Faker _faker = new Faker();

        public AccountStatusTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Get_activated_account()
        {
            using var server = CreateServer();

            await using var context = server.Services.GetService<OAuthContext>();
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()), 
                new CultureInfo("en"), 
                new Uri(_faker.Image.LoremPixelUrl())
            );
            var validator = server.Services.GetService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, validator);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var id = account.Identities.First().Id;
            var httpResponse = await server.CreateClient()
                .GetAsync($"identities/{id}/status");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<IdentityActiveResponse>(json);
            Assert.NotNull(response);
            Assert.True(response.IsActive);
        }

        [Fact]
        public async Task Get_non_confirmed_deactivated_account()
        {
            using var server = CreateServer();

            await using var context = server.Services.GetService<OAuthContext>();
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()), 
                new CultureInfo("en"), 
                new Uri(_faker.Image.LoremPixelUrl())
            );
            var validator = server.Services.GetService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, false, validator);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var id = account.Identities.First().Id;
            var httpResponse = await server.CreateClient()
                .GetAsync($"identities/{id}/status");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<IdentityActiveResponse>(json);
            Assert.NotNull(response);
            Assert.False(response.IsActive);
        }

        [Fact]
        public async Task Get_banned_deactivated_account()
        {
            using var server = CreateServer();

            await using var context = server.Services.GetService<OAuthContext>();
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()), 
                new CultureInfo("en"), 
                new Uri(_faker.Image.LoremPixelUrl())
            );
            var validator = server.Services.GetService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, validator);
            account.SetBan(new Ban(BanType.Temporarily, DateTimeOffset.Now, _faker.Date.Future(), "Test reason"));

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var id = account.Identities.First().Id;
            var httpResponse = await server.CreateClient()
                .GetAsync($"identities/{id}/status");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<IdentityActiveResponse>(json);
            Assert.NotNull(response);
            Assert.False(response.IsActive);
        }
        
        [Fact]
        public async Task Get_non_available_deactivated_account()
        {
            using var server = CreateServer();

            var id = _faker.Random.Guid();
            var httpResponse = await server.CreateClient()
                .GetAsync($"identities/{id}/status");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<IdentityActiveResponse>(json);
            Assert.NotNull(response);
            Assert.False(response.IsActive);
        }
    }
}