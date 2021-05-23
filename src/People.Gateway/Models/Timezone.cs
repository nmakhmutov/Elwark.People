using System;

namespace People.Gateway.Models
{
    internal sealed record Timezone(string Name, TimeSpan Offset);
}