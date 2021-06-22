using System;
using MediatR;
using People.Domain.Aggregates.Account;

namespace People.Domain.Events
{
    public sealed record AccountUpdatedDomainEvent(AccountId Id, DateTime UpdatedAt) : INotification;
}
