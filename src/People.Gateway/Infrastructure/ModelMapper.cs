using System;
using DayOfWeek = People.Grpc.Common.DayOfWeek;

namespace People.Gateway.Infrastructure;

internal static class ModelMapper
{
    internal static System.DayOfWeek FromGrpc(this DayOfWeek dayOfWeek) =>
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
