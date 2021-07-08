using System;

namespace People.Gateway.Features.Timezone
{
    internal sealed record Timezone(string Name, TimeSpan Offset);
}
