using System;
using DayOfWeek = People.Grpc.Common.DayOfWeek;

namespace Gateway.Api.Mappes;

internal static class CommonMapper
{
    public static DayOfWeek ToGrpc(this System.DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            System.DayOfWeek.Sunday => DayOfWeek.Sunday,
            System.DayOfWeek.Monday => DayOfWeek.Monday,
            System.DayOfWeek.Tuesday => DayOfWeek.Tuesday,
            System.DayOfWeek.Wednesday => DayOfWeek.Wednesday,
            System.DayOfWeek.Thursday => DayOfWeek.Thursday,
            System.DayOfWeek.Friday => DayOfWeek.Friday,
            System.DayOfWeek.Saturday => DayOfWeek.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
        };

    public static System.DayOfWeek FromGrpc(this DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Sunday => System.DayOfWeek.Sunday,
            DayOfWeek.Monday => System.DayOfWeek.Monday,
            DayOfWeek.Tuesday => System.DayOfWeek.Tuesday,
            DayOfWeek.Wednesday => System.DayOfWeek.Wednesday,
            DayOfWeek.Thursday => System.DayOfWeek.Thursday,
            DayOfWeek.Friday => System.DayOfWeek.Friday,
            DayOfWeek.Saturday => System.DayOfWeek.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
        };
}
