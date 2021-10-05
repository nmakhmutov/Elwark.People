using System;

namespace Gateway.Api.Features.Timezone;

internal sealed record TimeZone(string Name, TimeSpan Offset);
