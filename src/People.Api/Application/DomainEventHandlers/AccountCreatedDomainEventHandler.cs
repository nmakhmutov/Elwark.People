using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.Events;
using People.Infrastructure.Integration;

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
            notification.Account.GetCreatedDateTime(),
            notification.Account.Id,
            notification.IpAddress.ToString()
        );

        return _bus.PublishAsync(evt, ct);
    }
}
