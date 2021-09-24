using System;
using System.Collections.Generic;
using People.Grpc.Common;
using Address = People.Gateway.Models.Address;
using DayOfWeek = System.DayOfWeek;

namespace People.Gateway.Features.Profile
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
        string TimeZone,
        DayOfWeek FirstDayOfWeek,
        Ban? Ban,
        bool IsPasswordAvailable,
        DateTime CreatedAt,
        IEnumerable<Connection> Connections
    );
}
