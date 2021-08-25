using System;
using System.Collections.Generic;
using People.Grpc.Common;

namespace People.Gateway.Models
{
    internal sealed record Profile(
        long Id,
        string Nickname,
        bool PreferNickname,
        string? FirstName,
        string? LastName,
        string FullName,
        string Language,
        Gender Gender,
        DateTime? DateOfBirth,
        string? Bio,
        string Picture,
        Address Address,
        TimeInfo TimeInfo,
        Ban? Ban,
        bool IsPasswordAvailable,
        DateTime CreatedAt,
        IEnumerable<Connection> Connections
    );
}
