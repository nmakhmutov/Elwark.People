using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.ProblemDetails;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elwark.People.Api.FunctionalTests
{
    public class AttachTest : ScenarioBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Faker _faker = new Faker();

        public AttachTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Attach_email_success()
        {
            var identityService = new FakeIdentityService();
            using var server = CreateServer(identityService);

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

            identityService.AccountIdGenerator = () => account.Id;
            var attachEmail = new Identification.Email(email.Value + "2");

            var httpResponse = await server.CreateClient()
                .PostAsync("accounts/me/attach/email",
                    new StringContent(JsonConvert.SerializeObject(attachEmail), Encoding.UTF8, "application/json")
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var identities = (await context.Accounts.Include(x => x.Identities).FirstAsync(x => x.Id == account.Id))
                .Identities;
            Assert.Contains(identities, x => x.Identification == email);
            Assert.Contains(identities, x => x.Identification == attachEmail);
        }

        [Fact]
        public async Task Attach_own_email_success()
        {
            var identityService = new FakeIdentityService();
            using var server = CreateServer(identityService);
            var validator = server.Services.GetService<IIdentificationValidator>();
            
            await using var context = server.Services.GetService<OAuthContext>();
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()),
                new CultureInfo("en"),
                new Uri(_faker.Image.LoremPixelUrl())
            );
            await account.AddIdentificationAsync(email, true, validator);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            identityService.AccountIdGenerator = () => account.Id;

            var httpResponse = await server.CreateClient()
                .PostAsync("accounts/me/attach/email",
                    new StringContent(JsonConvert.SerializeObject(email), Encoding.UTF8, "application/json")
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();
        }
        
        [Fact]
        public async Task Attach_other_exists_email_fail()
        {
            var identityService = new FakeIdentityService();
            using var server = CreateServer(identityService);
            var validator = server.Services.GetService<IIdentificationValidator>();
            
            await using var context = server.Services.GetService<OAuthContext>();
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()),
                new CultureInfo("en"),
                new Uri(_faker.Image.LoremPixelUrl())
            );
            await account.AddIdentificationAsync(email, true, validator);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();
            
            var email2 = new Identification.Email(_faker.Internet.Email());
            var account2 = new Account(
                new Name(email2.GetUser()),
                new CultureInfo("en"),
                new Uri(_faker.Image.LoremPixelUrl())
            );
            await account2.AddIdentificationAsync(email2, true, validator);

            context.Accounts.Update(account2);
            await context.SaveChangesAsync();

            identityService.AccountIdGenerator = () => account.Id;

            var httpResponse = await server.CreateClient()
                .PostAsync("accounts/me/attach/email",
                    new StringContent(JsonConvert.SerializeObject(email2), Encoding.UTF8, "application/json")
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            var response = JsonConvert.DeserializeObject<IdentificationProblemDetails>(json);
            Assert.Equal(nameof(IdentificationError), response.Title);
            Assert.Equal(IdentificationError.AlreadyRegistered.ToString(), response.Type);
            Assert.Equal(email2, response.Identifier);
        }
    }
}