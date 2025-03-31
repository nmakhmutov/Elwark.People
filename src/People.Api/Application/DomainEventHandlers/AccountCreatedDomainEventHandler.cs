using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.DomainEvents;
using People.Kafka.Integration;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountCreatedDomainEventHandler : INotificationHandler<AccountCreatedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;

    public AccountCreatedDomainEventHandler(IIntegrationEventBus bus) =>
        _bus = bus;

    public async Task Handle(AccountCreatedDomainEvent notification, CancellationToken ct)
    {
        var evt = new AccountCreatedIntegrationEvent(notification.Account.Id, notification.IpAddress.ToString());

        await _bus.PublishAsync(evt, ct);
    }
}
