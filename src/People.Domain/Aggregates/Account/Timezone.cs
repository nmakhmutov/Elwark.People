using System;

namespace People.Domain.Aggregates.Account
{
    public sealed record Timezone(string Name, TimeSpan Offset)
    {
        public static Timezone Default => new ("Etc/UTC", TimeSpan.Zero);
    }
}