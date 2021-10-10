using System.Collections.Generic;

namespace Gateway.Api.Features.AccountManagement.Models;

internal sealed record PageResponse<T>(IEnumerable<T> Items, uint Pages, long Count);
