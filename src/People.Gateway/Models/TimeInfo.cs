using System;
using People.Gateway.Features.Timezone;

namespace People.Gateway.Models
{
    internal sealed record TimeInfo(Timezone Timezone, DayOfWeek FirstDayOfWeek);
}
