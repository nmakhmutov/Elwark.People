using System;
using MediatR;
using People.Account.Domain.Aggregates.AccountAggregate;

namespace People.Account.Domain.Events
{
    public sealed record AccountUpdatedDomainEvent(AccountId Id, DateTime UpdatedAt) : INotification;
}
