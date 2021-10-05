using People.Grpc.Common;

namespace Gateway.Api.Features.Profile;

internal abstract record Connection(IdentityType IdentityType, string Value, bool IsConfirmed);

internal sealed record EmailConnection(IdentityType IdentityType, string Value, bool IsConfirmed, bool IsPrimary)
    : Connection(IdentityType, Value, IsConfirmed);

internal sealed record SocialConnection(IdentityType IdentityType, string Value, bool IsConfirmed, string? FirstName,
        string? LastName)
    : Connection(IdentityType, Value, IsConfirmed);
