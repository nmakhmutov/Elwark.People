using System;
using People.Grpc.Common;

namespace People.Gateway.Models
{
    internal sealed record Account(
        long Id,
        string Nickname,
        string? FirstName,
        string? LastName,
        string FullName,
        string Language,
        Gender Gender,
        DateTime? Birthday,
        string? Bio,
        string Picture,
        Address Address,
        Timezone Timezone,
        bool IsBanned
    );
}