using System;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Domain.Events
{
    public sealed record AccountUpdatedDomainEvent(AccountId Id, DateTime UpdatedAt) : INotification;
}
