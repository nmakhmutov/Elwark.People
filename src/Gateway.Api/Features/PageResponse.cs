using System.Collections.Generic;

namespace Gateway.Api.Features;

internal sealed record PageResponse<T>(IEnumerable<T> Items, uint Pages, long Count);
