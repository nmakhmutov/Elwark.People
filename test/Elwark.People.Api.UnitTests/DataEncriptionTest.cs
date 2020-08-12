using System;
using System.Security.Cryptography;
using Bogus;
using Elwark.People.Api.Infrastructure.Security;
using Xunit;

namespace Elwark.People.Api.UnitTests
{
    public class DataEncryptionTest
    {
        private class Data
        {
            public int IntProperty { get; set; } = 0;

            public long LongProperty { get; set; } = 0;

            public string StringProperty { get; set; } = string.Empty;
        }

        private const string Key = "key";
        private const string Iv = "12345678";
        private static readonly Faker Faker = new Faker();

        [Fact]
        public void Encrypt_Data_success()
        {
            var data = new Faker<Data>()
                .RuleFor(x => x.IntProperty, x => x.Random.Int())
                .RuleFor(x => x.LongProperty, x => x.Random.Long())
                .RuleFor(x => x.StringProperty, x => x.Random.String2(20))
                .Generate();

            var encryption = new DataEncryption(Key, Iv);
            var result = encryption.EncryptToString(data);

            Assert.NotNull(result);
            Assert.NotEqual(string.Empty, result);
        }

        [Fact]
        public void Decrypt_Data_success()
        {
            var data = new Faker<Data>()
                .RuleFor(x => x.IntProperty, x => x.Random.Int())
                .RuleFor(x => x.LongProperty, x => x.Random.Long())
                .RuleFor(x => x.StringProperty, x => x.Random.String2(20))
                .Generate();

            var encryption = new DataEncryption(Key, Iv);
            var result = encryption.EncryptToString(data);

            Assert.NotNull(result);
            Assert.NotEqual(string.Empty, result);

            var result2 = encryption.DecryptFromString<Data>(result);

            Assert.NotNull(result2);
            Assert.Equal(data.IntProperty, result2.IntProperty);
            Assert.Equal(data.LongProperty, result2.LongProperty);
            Assert.Equal(data.StringProperty, result2.StringProperty);
        }

        [Fact]
        public void Decrypt_non_base64_string_fail()
        {
            var encryption = new DataEncryption(Key, Iv);
            Assert.Throws<CryptographicException>(() => encryption.DecryptFromString<Data>(Faker.Random.String2(100)));
        }

        [Fact]
        public void Decrypt_incorrect_base64_string_fail()
        {
            var encryption = new DataEncryption(Key, Iv);
            Assert.Throws<CryptographicException>(() =>
                encryption.DecryptFromString<Data>(Convert.ToBase64String(Faker.Random.Bytes(20)))
            );
        }

        [Fact]
        public void Null_IV_fail()
        {
            Assert.Throws<ArgumentNullException>(() => new DataEncryption(Key, null));
        }

        [Fact]
        public void Null_Key_fail()
        {
            Assert.Throws<ArgumentNullException>(() => new DataEncryption(null, Iv));
        }

        [Fact]
        public void Null_Key_and_IV_fail()
        {
            Assert.Throws<ArgumentNullException>(() => new DataEncryption(null, null));
        }

        [Fact]
        public void Empty_IV_fail()
        {
            Assert.Throws<ArgumentNullException>(() => new DataEncryption(Key, string.Empty));
        }

        [Fact]
        public void Empty_Key_fail()
        {
            Assert.Throws<ArgumentNullException>(() => new DataEncryption(string.Empty, Iv));
        }

        [Fact]
        public void Empty_Key_and_IV_fail()
        {
            Assert.Throws<ArgumentNullException>(() => new DataEncryption(string.Empty, string.Empty));
        }
    }
}