using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Kafka;
using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.Events;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountCreatedDomainEventHandler : INotificationHandler<AccountCreatedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;

    public AccountCreatedDomainEventHandler(IIntegrationEventBus bus) =>
        _bus = bus;

    public Task Handle(AccountCreatedDomainEvent notification, CancellationToken ct)
    {
        var evt = new AccountCreatedIntegrationEvent(
            Guid.NewGuid(),
            notification.Account.CreatedAt,
            (long)notification.Account.Id,
            notification.Account.GetPrimaryEmail().Value,
            notification.IpAddress.ToString(),
            notification.Account.Language.ToString()
        );

        return _bus.PublishAsync(evt, ct);
    }
}
