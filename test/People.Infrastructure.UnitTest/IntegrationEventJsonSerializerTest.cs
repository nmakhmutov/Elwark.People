using System;
using Newtonsoft.Json;
using Integration.Event;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace People.Infrastructure.UnitTest;

public class IntegrationEventJsonSerializerTest
{
    [Fact]
    public void AccountCreatedIntegrationEvent_DeserializeTest()
    {
        var evt = new AccountCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1, "test@test.test", "::1",
            "en");

        var json = JsonConvert.SerializeObject(evt);
        var obj = JsonSerializer.Deserialize<AccountCreatedIntegrationEvent>(json);

        Assert.Equal(evt, obj);
    }

    [Fact]
    public void AccountCreatedIntegrationEvent_SerializeTest()
    {
        var evt = new AccountCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1, "test@test.test", "::1",
            "en");

        var json = JsonSerializer.Serialize(evt);
        var obj = JsonConvert.DeserializeObject<AccountCreatedIntegrationEvent>(json);

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

        var json = JsonConvert.SerializeObject(evt);
        var obj = JsonSerializer.Deserialize<AccountInfoReceivedIntegrationEvent>(json);

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

        var json = JsonSerializer.Serialize(evt);
        var obj = JsonConvert.DeserializeObject<AccountInfoReceivedIntegrationEvent>(json);

        Assert.Equal(evt, obj);
    }

    [Fact]
    public void AccountUpdatedIntegrationEvent_DeserializationTest()
    {
        var evt = new AccountUpdatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1);

        var json = JsonConvert.SerializeObject(evt);
        var obj = JsonSerializer.Deserialize<AccountUpdatedIntegrationEvent>(json);

        Assert.Equal(evt, obj);
    }

    [Fact]
    public void AccountUpdatedIntegrationEvent_SerializationTest()
    {
        var evt = new AccountUpdatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1);

        var json = JsonSerializer.Serialize(evt);
        var obj = JsonConvert.DeserializeObject<AccountUpdatedIntegrationEvent>(json);

        Assert.Equal(evt, obj);
    }

    [Fact]
    public void EmailMessageCreatedIntegrationEvent_DeserializationTest()
    {
        var evt = new EmailMessageCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "email",
            "subject",
            "body",
            true
        );

        var json = JsonConvert.SerializeObject(evt);
        var obj = JsonSerializer.Deserialize<EmailMessageCreatedIntegrationEvent>(json);

        Assert.Equal(evt, obj);
    }

    [Fact]
    public void EmailMessageCreatedIntegrationEvent_SerializationTest()
    {
        var evt = new EmailMessageCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "email",
            "subject",
            "body",
            true
        );

        var json = JsonSerializer.Serialize(evt);
        var obj = JsonConvert.DeserializeObject<EmailMessageCreatedIntegrationEvent>(json);

        Assert.Equal(evt, obj);
    }
}
