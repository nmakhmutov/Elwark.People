using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain;
using People.Domain.DomainEvents;
using People.Kafka.Integration;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountDeletedDomainEventHandler : INotificationHandler<AccountDeletedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;
    private readonly TimeProvider _timeProvider;

    public AccountDeletedDomainEventHandler(IIntegrationEventBus bus, TimeProvider timeProvider)
    {
        _bus = bus;
        _timeProvider = timeProvider;
    }

    public Task Handle(AccountDeletedDomainEvent notification, CancellationToken ct)
    {
        var evt = new AccountDeletedIntegrationEvent(Guid.NewGuid(), _timeProvider.UtcNow(), notification.Id);

        return _bus.PublishAsync(evt, ct);
    }
}
