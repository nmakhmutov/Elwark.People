using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain;
using People.Domain.DomainEvents;
using People.Kafka.Integration;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountUpdatedDomainEventHandler : INotificationHandler<AccountUpdatedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;
    private readonly TimeProvider _timeProvider;

    public AccountUpdatedDomainEventHandler(IIntegrationEventBus bus, TimeProvider timeProvider)
    {
        _bus = bus;
        _timeProvider = timeProvider;
    }

    public Task Handle(AccountUpdatedDomainEvent notification, CancellationToken ct)
    {
        var evt = new AccountUpdatedIntegrationEvent(
            Guid.NewGuid(),
            _timeProvider.UtcNow(),
            notification.Account.Id
        );

        return _bus.PublishAsync(evt, ct);
    }
}
