using People.Grpc.Common;

namespace People.Gateway.Requests
{
    public sealed record ChangeEmailType(string Email, EmailType Type);
}