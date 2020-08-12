using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Shared.Primitives;
using Xunit;

namespace Elwark.People.Api.UnitTests.Domain
{
    public class AccountAggregateTest
    {
        private static readonly Faker Faker = new Faker();

        [Theory, MemberData(nameof(Accounts))]
        public void Create_internal_account_success(Account account)
        {
            Assert.NotNull(account.Name);
            Assert.NotNull(account.BasicInfo);
            Assert.NotNull(account.BasicInfo.Timezone);
            Assert.NotNull(account.Address);
            Assert.NotNull(account.Links);
            Assert.NotNull(account.Picture);
            Assert.NotEmpty(account.DomainEvents);
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Add_external_account_success(Account account)
        {
            const string providerId = "1234567890";

            account.AddIdentificationAsync(new Identification.Google(providerId), false, new IdentificationFake());

            Assert.NotNull(account);
            Assert.NotEmpty(account.Identities);
            Assert.NotEmpty(account.DomainEvents);
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Update_information_success(Account account)
        {
            account.ClearDomainEvents();

            var firstName = Faker.Person.FirstName;
            var lastName = Faker.Person.LastName;
            var nickname = Faker.Internet.UserName();
            var gender = Faker.PickRandom<Gender>();
            var birthday = Faker.Date.Past();
            var country = Faker.Address.CountryCode();
            var city = Faker.Address.City();
            var timezone = Faker.Random.String2(10);
            var picture = Faker.Image.LoremFlickrUrl().ToUri();
            var bio = Faker.Random.String2(100);
            var language = new CultureInfo(Faker.Internet.Locale);
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
            
            account.SetName(new Name(nickname, firstName, lastName));
            account.SetBasicInfo(new BasicInfo(language, gender, timezone, birthday, bio));
            account.SetAddress(new Address(country, city));
            account.SetLinks(links);
            account.SetPicture(picture);

            Assert.Equal(firstName, account.Name.FirstName);
            Assert.Equal(lastName, account.Name.LastName);
            Assert.Equal(nickname, account.Name.Nickname);

            Assert.Equal(gender, account.BasicInfo.Gender);
            Assert.Equal(birthday, account.BasicInfo.Birthday);
            Assert.Equal(timezone, account.BasicInfo.Timezone);
            Assert.Equal(language, account.BasicInfo.Language);
            Assert.Equal(bio, account.BasicInfo.Bio);

            Assert.Equal(city, account.Address.City);
            Assert.Equal(country, account.Address.CountryCode);

            Assert.Equal(picture, account.Picture);

            Assert.Equal(links, account.Links);
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Add_ban_success(Account account)
        {
            var ban = new Ban(BanType.Permanent, DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1), "Test");
            account.SetBan(ban);

            Assert.NotNull(account.Ban);
            Assert.Equal(BanType.Permanent, account.Ban.Type);
            Assert.True(account.IsBanned());
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Add_null_ban_error(Account account)
        {
            Assert.Throws<ArgumentNullException>(() => account.SetBan(null));
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Remove_ban_success(Account account)
        {
            var ban = new Ban(BanType.Permanent, DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1), "Test");
            account.SetBan(ban);

            account.RemoveBan();
            Assert.Null(account.Ban);
            Assert.False(account.IsBanned());
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Add_roles_success(Account account)
        {
            var roles = new[] {"role1", "role2", "role3"};

            foreach (var role in roles)
                account.AddRole(role);

            Assert.NotNull(account.Roles);
            Assert.Equal(3, account.Roles.Count);
            Assert.Collection(account.Roles,
                role => Assert.Equal("role1", role),
                role => Assert.Equal("role2", role),
                role => Assert.Equal("role3", role)
            );
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Remove_roles_success(Account account)
        {
            var roles = new[] {"role1", "role2", "role3"};

            foreach (var role in roles)
                account.AddRole(role);

            Assert.Equal(3, account.Roles.Count);

            foreach (var role in roles)
                account.RemoveRole(role);

            Assert.Equal(0, account.Roles.Count);
        }

        [Theory, MemberData(nameof(Accounts))]
        public void Skip_duplicated_roles_success(Account account)
        {
            var roles = new[] {"role1", "role2", "role3", "role3"};

            foreach (var role in roles)
                account.AddRole(role);

            Assert.NotNull(account.Roles);
            Assert.Equal(3, account.Roles.Count);
        }

        public static IEnumerable<object[]> Accounts => new[]
        {
            new object[]
            {
                new Account(
                    new Name(Faker.Internet.UserName()),
                    new CultureInfo("en"),
                    new Uri(Faker.Image.PicsumUrl())
                )
            },
            new object[]
            {
                new Account(
                    new Name(Faker.Internet.UserName()),
                    new BasicInfo(new CultureInfo("en")),
                    new Uri(Faker.Image.PicsumUrl()),
                    new Links()
                )
            },
        };
        
        private class IdentificationFake : IIdentificationValidator
        {
            public Task CheckUniquenessAsync(Identification identification, CancellationToken ct) =>
                Task.CompletedTask;
        }
    }
}