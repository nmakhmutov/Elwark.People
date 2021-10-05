using System.Collections.Generic;

namespace People.Api.Application.Models;

public sealed record PaginatedCollection<T>(IEnumerable<T> Items, uint Pages, long Count);
