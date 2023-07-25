using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain;
using People.Domain.DomainEvents;
using People.Kafka.Integration;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountCreatedDomainEventHandler : INotificationHandler<AccountCreatedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;
    private readonly TimeProvider _timeProvider;

    public AccountCreatedDomainEventHandler(IIntegrationEventBus bus, TimeProvider timeProvider)
    {
        _bus = bus;
        _timeProvider = timeProvider;
    }

    public Task Handle(AccountCreatedDomainEvent notification, CancellationToken ct)
    {
        var evt = new AccountCreatedIntegrationEvent(
            Guid.NewGuid(),
            _timeProvider.UtcNow(),
            notification.Account.Id,
            notification.IpAddress.ToString()
        );

        return _bus.PublishAsync(evt, ct);
    }
}
