using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Infrastructure;
using Elwark.People.Shared.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elwark.People.Api.FunctionalTests.DbContext
{
    public class OAuthDbContextTest : ScenarioBase
    {
        private static readonly Faker Faker = new Faker();

        [Fact]
        public async Task Create_new_account_success()
        {
            var nickname = Faker.Random.String2(20);
            var culture = Faker.PickRandom(new CultureInfo("en"), new CultureInfo("fr"), new CultureInfo("gb"));
            var picture = Faker.Image.PicsumUrl().ToUri();
            var account = new Account(new Name(nickname), culture, picture!);

            var role = Faker.Random.String2(8);
            account.AddRole(role);

            var identifiers = Faker.PickRandom(
                    new Identification[]
                    {
                        new Identification.Facebook(Faker.Random.Hash()),
                        new Identification.Google(Faker.Random.Hash()),
                        new Identification.Microsoft(Faker.Random.Hash())
                    });

            var ban = new Ban(BanType.Temporarily, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
                Faker.Lorem.Lines(1));
            account.SetBan(ban);

            using var server = CreateServer();
            var hasher = server.Services.GetRequiredService<IPasswordHasher>();
            var passwordValidator = server.Services.GetRequiredService<IPasswordValidator>();
            var notificationValidator = server.Services.GetRequiredService<IIdentificationValidator>();

            await account.AddIdentificationAsync(new Identification.Email(Faker.Internet.Email()), true, notificationValidator);
            await account.AddIdentificationAsync(identifiers, true, notificationValidator);
            
            var password = Faker.Internet.Password();
            await account.SetPasswordAsync(password, passwordValidator, hasher);
            
            var links = new Links
            {
                {LinksType.Dribbble, new Uri(Faker.Image.LoremPixelUrl())},
                {LinksType.Facebook, new Uri(Faker.Image.LoremPixelUrl())},
                {LinksType.Medium, new Uri(Faker.Image.LoremPixelUrl())},
                {LinksType.Twitter, new Uri(Faker.Image.LoremPixelUrl())},
                {LinksType.Github, new Uri(Faker.Image.LoremPixelUrl())},
                {LinksType.LinkedIn, new Uri(Faker.Image.LoremPixelUrl())},
                {LinksType.Website, new Uri(Faker.Image.LoremPixelUrl())},
            };

            account.SetLinks(links);

            await using var context = server.Services.GetRequiredService<OAuthContext>();
            await context.AddAsync(account);
            await context.SaveChangesAsync();

            var db = await context.Accounts.FirstOrDefaultAsync(x => x.Id == account.Id);

            Assert.NotNull(db);
            Assert.True(db.Id > 0);
            Assert.Equal(ban, db.Ban);

            Assert.NotNull(db.BasicInfo);
            Assert.Equal(account.BasicInfo.Bio, db.BasicInfo.Bio);
            Assert.Equal(account.BasicInfo.Birthday, db.BasicInfo.Birthday);
            Assert.Equal(account.BasicInfo.Gender, db.BasicInfo.Gender);
            Assert.Equal(account.BasicInfo.Language, db.BasicInfo.Language);
            Assert.Equal(account.BasicInfo.Timezone, db.BasicInfo.Timezone);

            Assert.NotNull(db.Address);
            Assert.Equal(account.Address.City, db.Address.City);
            Assert.Equal(account.Address.CountryCode, db.Address.CountryCode);

            Assert.NotNull(db.Name);
            Assert.Equal(account.Name.Nickname, db.Name.Nickname);
            Assert.Equal(account.Name.FirstName, db.Name.FirstName);
            Assert.Equal(account.Name.LastName, db.Name.LastName);

            Assert.NotNull(db.Links);
            Assert.Equal(account.Links, db.Links);

            Assert.NotNull(db.Password);
            Assert.NotEmpty(db.Password?.Hash ?? Array.Empty<byte>());
            Assert.NotEmpty(db.Password?.Salt ?? Array.Empty<byte>());

            Assert.Equal(account.Picture, db.Picture);

            Assert.Contains(db.Roles, s => s == role);

            Assert.True(db.Identities.Count > 0);
            foreach (var identifier in account.Identities)
            {
                var first = db.Identities.First(x => x.Id == identifier.Id);
                Assert.True(first.AccountId > 0);
                Assert.False(first.Id == Guid.Empty);
                Assert.NotNull(first.ConfirmedAt);
                if (first.Identification is Identification.Email)
                {
                    Assert.True(first.IsConfirmed);
                    Assert.True(first.NotificationType == NotificationType.PrimaryEmail);
                }
                else
                {
                    Assert.True(first.IsConfirmed);
                    Assert.True(first.NotificationType == NotificationType.None);
                }
            }
        }
    }
}