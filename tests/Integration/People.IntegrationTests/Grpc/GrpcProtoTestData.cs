using People.Grpc.People;

namespace People.IntegrationTests.Grpc;

internal static class GrpcProtoTestData
{
    public static IpAddress LoopbackIp() =>
        new()
        {
            Value = "127.0.0.1"
        };

    public static UserAgent TestUserAgent() =>
        new()
        {
            Value = "grpc-integration-test"
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
