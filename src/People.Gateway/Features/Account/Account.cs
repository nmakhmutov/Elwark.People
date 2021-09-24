using System;
using People.Grpc.Common;
using Address = People.Gateway.Models.Address;
using DayOfWeek = System.DayOfWeek;

namespace People.Gateway.Features.Account
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
        string TimeZone,
        DayOfWeek FirstDayOfWeek,
        bool IsBanned
    );
}
