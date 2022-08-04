using MediatR;
using People.Domain.Entities;

namespace People.Domain.DomainEvents;

public sealed record AccountBannedDomainEvent(AccountId Id, string Reason, DateTime ExpiredAt) : INotification;
