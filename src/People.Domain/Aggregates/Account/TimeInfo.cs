using System;

namespace People.Domain.Aggregates.Account
{
    public sealed record TimeInfo(Timezone Timezone, DayOfWeek FirstDayOfWeek)
    {
        public static TimeInfo Default => new(Timezone.Default, DayOfWeek.Monday);
    }
}
