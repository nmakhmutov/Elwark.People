using People.Grpc.People;

namespace Integration.Api.Tests.Grpc;

internal static class GrpcProtoTestData
{
    public static Metadata TestMetadata() =>
        new()
        {
            IpAddress = "127.0.0.1",
            UserAgent = "grpc-integration-test",
            Timezone = TimeZoneInfo.Utc.Id,
            Locale = "en"
        };

    public static Language EnLanguage() =>
        new()
        {
            Value = "en"
        };

    public static Email Email(string address) =>
        new()
        {
            Value = address
        };
}
