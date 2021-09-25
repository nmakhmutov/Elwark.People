using System;

namespace People.Gateway.Mappes
{
    internal static class CommonMapper
    {
        public static People.Grpc.Common.DayOfWeek ToGrpc(this DayOfWeek dayOfWeek) =>
            dayOfWeek switch
            {
                DayOfWeek.Sunday => Grpc.Common.DayOfWeek.Sunday,
                DayOfWeek.Monday => Grpc.Common.DayOfWeek.Monday,
                DayOfWeek.Tuesday => Grpc.Common.DayOfWeek.Tuesday,
                DayOfWeek.Wednesday => Grpc.Common.DayOfWeek.Wednesday,
                DayOfWeek.Thursday => Grpc.Common.DayOfWeek.Thursday,
                DayOfWeek.Friday => Grpc.Common.DayOfWeek.Friday,
                DayOfWeek.Saturday => Grpc.Common.DayOfWeek.Saturday,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
        
        public static DayOfWeek FromGrpc(this People.Grpc.Common.DayOfWeek dayOfWeek) =>
            dayOfWeek switch
            {
                Grpc.Common.DayOfWeek.Sunday => DayOfWeek.Sunday,
                Grpc.Common.DayOfWeek.Monday => DayOfWeek.Monday,
                Grpc.Common.DayOfWeek.Tuesday => DayOfWeek.Tuesday,
                Grpc.Common.DayOfWeek.Wednesday => DayOfWeek.Wednesday,
                Grpc.Common.DayOfWeek.Thursday => DayOfWeek.Thursday,
                Grpc.Common.DayOfWeek.Friday => DayOfWeek.Friday,
                Grpc.Common.DayOfWeek.Saturday => DayOfWeek.Saturday,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
    }
}
