using System;
using People.Grpc.Common;

namespace People.Gateway.Models
{
    public sealed record Account(
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
        Email Email,
        Address Address,
        Timezone Timezone,
        bool IsBanned
    );

    public record Email(string Value, bool IsConfirmed);

    public record Timezone(string Name, TimeSpan Offset);

    public record Address(string? CountryCode, string? CityName);
}