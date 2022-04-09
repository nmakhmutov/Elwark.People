using System.Collections.Generic;

namespace People.Gateway.Features;

internal sealed record PageResponse<T>(IEnumerable<T> Items, uint Pages, long Count);
