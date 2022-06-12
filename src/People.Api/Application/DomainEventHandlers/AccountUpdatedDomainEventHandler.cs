using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.Events;
using People.Infrastructure.Integration;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountUpdatedDomainEventHandler : INotificationHandler<AccountUpdatedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;

    public AccountUpdatedDomainEventHandler(IIntegrationEventBus bus) =>
        _bus = bus;

    public Task Handle(AccountUpdatedDomainEvent notification, CancellationToken ct)
    {
        var evt = new AccountUpdatedIntegrationEvent(
            Guid.NewGuid(),
            notification.Account.GetUpdatedDateTime(),
            notification.Account.Id
        ); 
        
        return _bus.PublishAsync(evt, ct);
    }
}
