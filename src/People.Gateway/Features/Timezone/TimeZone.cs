using System;

namespace People.Gateway.Features.Timezone
{
    internal sealed record TimeZone(string Name, TimeSpan Offset);
}
