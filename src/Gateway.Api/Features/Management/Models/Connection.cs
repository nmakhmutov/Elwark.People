using System;
using People.Grpc.Common;

namespace Gateway.Api.Features.Management.Models;

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
