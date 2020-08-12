using System;
using Bogus;
using Elwark.People.Abstractions;
using Elwark.People.Shared;
using Newtonsoft.Json;
using Xunit;

namespace Elwark.People.Api.UnitTests
{
    public class IdentificationJsonTest
    {
        private static readonly Faker Faker = new Faker();

        [Fact]
        public void Serialize_deserialize_success()
        {
            var email = new Identification.Email(Faker.Internet.Email());
            var json = JsonConvert.SerializeObject(email, ElwarkJsonSettings.Value);

            Assert.NotNull(json);

            var newEmail = JsonConvert.DeserializeObject<Identification>(json);

            Assert.NotNull(newEmail);

            Assert.IsType(email.GetType(), newEmail);
            Assert.Equal(email, newEmail);
        }

        [Fact]
        public void Serialize_success()
        {
            var email = Faker.Internet.Email().ToLower();
            var emailString = $"\"{IdentificationType.Email:G}:{email}\"";

            var json = JsonConvert.SerializeObject(Identification.Create(IdentificationType.Email, email));

            Assert.Equal(emailString, json);
        }

        [Fact]
        public void Deserialize_success()
        {
            var email = Faker.Internet.Email().ToLower();
            var json = $"\"{IdentificationType.Email:G}:{email}\"";

            var identifier = JsonConvert.DeserializeObject<Identification>(json);


            Assert.Equal(new Identification.Email(email), identifier);
        }

        [Fact]
        public void Deserialize_without_separator_fail()
        {
            var email = Faker.Internet.Email().ToLower();
            var json = $"\"{IdentificationType.Email:G}{email}\"";

            Assert.Throws<ArgumentException>(() => JsonConvert.DeserializeObject<Identification>(json));
        }
        
        [Fact]
        public void Deserialize_with_unknown_type_fail()
        {
            var email = Faker.Internet.Email().ToLower();
            var json = $"\"wrong_type:{email}\"";

            Assert.Throws<ArgumentException>(() => JsonConvert.DeserializeObject<Identification>(json));
        }
        
        [Fact]
        public void Deserialize_without_value_fail()
        {
            var json = $"\"{IdentificationType.Email:G}:\"";

            Assert.Throws<ArgumentException>(() => JsonConvert.DeserializeObject<Identification>(json));
        }
        
        [Fact]
        public void Deserialize_without_type_fail()
        {
            var email = Faker.Internet.Email().ToLower();
            var json = $"\":{email}\"";

            Assert.Throws<ArgumentException>(() => JsonConvert.DeserializeObject<Identification>(json));
        }
    }
}