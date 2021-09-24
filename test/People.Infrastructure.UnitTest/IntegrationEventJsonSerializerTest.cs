using System;
using People.Integration.Event;
using Xunit;

namespace People.Infrastructure.UnitTest
{
    public class IntegrationEventJsonSerializerTest
    {
        [Fact]
        public void AccountCreatedIntegrationEvent_SerializeTest()
        {
            var evt = new AccountCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1, "test@test.test", "::1",
                "en");

            var json = System.Text.Json.JsonSerializer.Serialize(evt);
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<AccountCreatedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }

        [Fact]
        public void AccountCreatedIntegrationEvent_DeserializeTest()
        {
            var evt = new AccountCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1, "test@test.test", "::1",
                "en");

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(evt);
            var obj = System.Text.Json.JsonSerializer.Deserialize<AccountCreatedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }

        [Fact]
        public void AccountInfoReceivedIntegrationEvent_SerializeTest()
        {
            var evt = new AccountInfoReceivedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                1,
                "::1",
                "us",
                null,
                null,
                "name",
                "not name",
                null,
                new Uri("http://localhost")
            );

            var json = System.Text.Json.JsonSerializer.Serialize(evt);
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<AccountInfoReceivedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }

        [Fact]
        public void AccountInfoReceivedIntegrationEvent_DeserializeTest()
        {
            var evt = new AccountInfoReceivedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                1,
                "::1",
                "us",
                null,
                null,
                "name",
                "not name",
                null,
                new Uri("http://localhost")
            );

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(evt);
            var obj = System.Text.Json.JsonSerializer.Deserialize<AccountInfoReceivedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }

        [Fact]
        public void AccountUpdatedIntegrationEvent_SerializationTest()
        {
            var evt = new AccountUpdatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1);

            var json = System.Text.Json.JsonSerializer.Serialize(evt);
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<AccountUpdatedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }

        [Fact]
        public void AccountUpdatedIntegrationEvent_DeserializationTest()
        {
            var evt = new AccountUpdatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(evt);
            var obj = System.Text.Json.JsonSerializer.Deserialize<AccountUpdatedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }

        [Fact]
        public void EmailMessageCreatedIntegrationEvent_SerializationTest()
        {
            var evt = new EmailMessageCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "email", "subject",
                "body");

            var json = System.Text.Json.JsonSerializer.Serialize(evt);
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<EmailMessageCreatedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }

        [Fact]
        public void EmailMessageCreatedIntegrationEvent_DeserializationTest()
        {
            var evt = new EmailMessageCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "email", "subject",
                "body");

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(evt);
            var obj = System.Text.Json.JsonSerializer.Deserialize<EmailMessageCreatedIntegrationEvent>(json);

            Assert.Equal(evt, obj);
        }
    }
}
