using System;

namespace People.Domain.Aggregates.AccountAggregate
{
    public sealed record TimeInfo(Timezone Timezone, DayOfWeek FirstDayOfWeek)
    {
        public static TimeInfo Default => new(Timezone.Default, DayOfWeek.Monday);
    }
}
