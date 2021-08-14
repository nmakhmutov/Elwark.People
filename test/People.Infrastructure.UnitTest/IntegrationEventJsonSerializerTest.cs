using System;
using People.Domain.Aggregates.EmailProvider;
using People.Infrastructure.IntegrationEvents;
using Xunit;

namespace People.Infrastructure.UnitTest;

public class IntegrationEventJsonSerializerTest
{
    [Fact]
    public void AccountCreatedIntegrationEvent_SerializeTest()
    {
        var evt = new AccountCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, 1, "test@test.test", "::1", "en");

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(evt);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(evt);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void AccountCreatedIntegrationEvent_DeserializeTest()
    {
        const string evt =
            "{\"MessageId\":\"44f5000c-fb76-4062-a50e-585abeed7f91\",\"CreatedAt\":\"2021-08-19T11:52:08.677534Z\",\"AccountId\":1,\"Email\":\"test@test.test\",\"Ip\":\"::1\",\"Language\":\"en\"}";

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.DeserializeObject<AccountCreatedIntegrationEvent>(evt);
        var systemTextJson = System.Text.Json.JsonSerializer.Deserialize<AccountCreatedIntegrationEvent>(evt);

        Assert.Equal(newtonsoftJson, systemTextJson);
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

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(evt);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(evt);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void AccountInfoReceivedIntegrationEvent_DeserializeTest()
    {
        const string evt =
            "{\"MessageId\":\"d52e8c38-ba75-4fce-b4af-88ba3dda7d07\",\"CreatedAt\":\"2021-08-19T11:56:30.393241Z\",\"AccountId\":1,\"Ip\":\"::1\",\"CountryCode\":\"us\",\"City\":null,\"Timezone\":null,\"FirstName\":\"name\",\"LastName\":\"not name\",\"AboutMe\":null,\"Image\":\"http://localhost\"}";

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.DeserializeObject<AccountInfoReceivedIntegrationEvent>(evt);
        var systemTextJson = System.Text.Json.JsonSerializer.Deserialize<AccountInfoReceivedIntegrationEvent>(evt);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void ProviderExpiredIntegrationEvent_SerializationTest()
    {
        var evt = new ProviderExpiredIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, EmailProviderType.Gmail);

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(evt);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(evt);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void ProviderExpiredIntegrationEvent_DeserializationTest()
    {
        const string evt =
            "{\"MessageId\":\"a48a0185-4639-499c-aa4a-a76bf1da2585\",\"CreatedAt\":\"2021-08-19T12:00:29.006728Z\",\"Type\":2}";

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.DeserializeObject<ProviderExpiredIntegrationEvent>(evt);
        var systemTextJson = System.Text.Json.JsonSerializer.Deserialize<ProviderExpiredIntegrationEvent>(evt);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }
}
