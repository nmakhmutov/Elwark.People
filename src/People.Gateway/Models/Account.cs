using System;
using People.Grpc.Common;
using Timezone = People.Gateway.Features.Timezone.Timezone;

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
        DateTime? DateOfBirth,
        string? Bio,
        string Picture,
        Address Address,
        Timezone Timezone,
        bool IsBanned
    );
}
