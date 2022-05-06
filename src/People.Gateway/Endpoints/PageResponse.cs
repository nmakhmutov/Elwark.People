using System.Collections.Generic;

namespace People.Gateway.Endpoints;

internal sealed record PageResponse<T>(IEnumerable<T> Items, uint Pages, long Count);
