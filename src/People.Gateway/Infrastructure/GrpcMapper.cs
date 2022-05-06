using System;
using DayOfWeek = People.Grpc.Common.DayOfWeek;

namespace People.Gateway.Infrastructure;

internal static class GrpcMapper
{
    internal static DayOfWeek ToGrpc(this System.DayOfWeek dayOfWeek) =>
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
}
