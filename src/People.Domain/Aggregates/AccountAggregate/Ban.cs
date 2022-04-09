using System;

namespace People.Domain.Aggregates.AccountAggregate;

public sealed record Ban(string Reason, DateTime CreatedAt, DateTime ExpiredAt);
