using System;

namespace People.Domain.AggregateModels.Account
{
    public sealed record Timezone(string Name, TimeSpan Offset)
    {
        public static Timezone Default => new ("Etc/UTC", TimeSpan.Zero);
    }
}