using System;
using People.Grpc.Common;

namespace People.Gateway.Endpoints.Management.Model;

internal abstract record Connection(IdentityType Type, string Value, DateTime CreatedAt, DateTime? ConfirmedAt);

internal sealed record EmailConnection(
    IdentityType Type,
    string Value,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    bool IsPrimary
) : Connection(Type, Value, CreatedAt, ConfirmedAt);

internal sealed record SocialConnection(
    IdentityType Type,
    string Value,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    string? FirstName,
    string? LastName
) : Connection(Type, Value, CreatedAt, ConfirmedAt);
