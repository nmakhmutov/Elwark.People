using People.Grpc.Common;

namespace People.Gateway.Models
{
    internal abstract record Connection(IdentityType IdentityType, string Value, bool IsConfirmed);

    internal sealed record EmailConnection(IdentityType IdentityType, string Value, bool IsConfirmed, EmailType EmailType)
        : Connection(IdentityType, Value, IsConfirmed);

    internal sealed record SocialConnection(IdentityType IdentityType, string Value, bool IsConfirmed, string? FirstName,
            string? LastName)
        : Connection(IdentityType, Value, IsConfirmed);
}
