using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.DomainEvents;
using People.Kafka.Integration;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountDeletedDomainEventHandler : INotificationHandler<AccountDeletedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;

    public AccountDeletedDomainEventHandler(IIntegrationEventBus bus) =>
        _bus = bus;

    public Task Handle(AccountDeletedDomainEvent notification, CancellationToken ct)
    {
        var evt = new AccountDeletedIntegrationEvent(notification.Id);

        return _bus.PublishAsync(evt, ct);
    }
}
