using People.Grpc.Common;

namespace People.Gateway.Models
{
    internal abstract record Identity(IdentityType IdentityType, string Value, bool IsConfirmed);
    
    internal sealed record EmailIdentity(IdentityType IdentityType, string Value, bool IsConfirmed, EmailType EmailType)
        : Identity(IdentityType, Value, IsConfirmed);

    internal sealed record SocialIdentity(IdentityType IdentityType, string Value, bool IsConfirmed, string Name)
        : Identity(IdentityType, Value, IsConfirmed);
}