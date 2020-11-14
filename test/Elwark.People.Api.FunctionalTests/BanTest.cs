using System;
using System.Globalization;
using System.Threading.Tasks;
using Bogus;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Infrastructure;
using Elwark.People.Shared.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elwark.People.Api.FunctionalTests
{
    public class BanTest : ScenarioBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Faker _faker = new Faker();

        public BanTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Get_ban_from_banned_account()
        {
            using var server = CreateServer();

            await using var context = server.Services.GetRequiredService<OAuthContext>();
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()),
                new CultureInfo("en"),
                new Uri(_faker.Image.LoremPixelUrl())
            );
            var validator = server.Services.GetRequiredService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, validator);

            var ban = new Ban(BanType.Temporarily, _faker.Date.Past().Date, _faker.Date.Future().Date,
                _faker.Random.String2(20));
            account.SetBan(ban);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var httpResponse = await server.CreateClient()
                .GetAsync($"accounts/{account.Id}/ban");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<BanModel>(json);
            Assert.NotNull(response);
            Assert.True(response.IsBanned);

            Assert.NotNull(response.Details);
            Assert.Equal(ban.Type, response.Details!.Type);
            Assert.Equal(ban.Reason, response.Details.Reason);
            Assert.Equal(ban.CreatedAt, response.Details.CreatedAt);
            Assert.Equal(ban.ExpiredAt, response.Details.ExpiredAt);
        }

        [Fact]
        public async Task Get_ban_from_not_banned_account()
        {
            using var server = CreateServer();

            await using var context = server.Services.GetRequiredService<OAuthContext>();
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()),
                new CultureInfo("en"),
                new Uri(_faker.Image.LoremPixelUrl())
            );
            var validator = server.Services.GetRequiredService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, validator);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var httpResponse = await server.CreateClient()
                .GetAsync($"accounts/{account.Id}/ban");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<BanModel>(json);
            Assert.NotNull(response);
            Assert.False(response.IsBanned);
            Assert.Null(response.Details);
        }
    }
}