using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Kafka;
using Integration.Event;
using MediatR;
using People.Domain.Events;

namespace People.Api.Application.DomainEventHandlers;

internal sealed class AccountUpdatedDomainEventHandler : INotificationHandler<AccountUpdatedDomainEvent>
{
    private readonly IKafkaMessageBus _bus;

    public AccountUpdatedDomainEventHandler(IKafkaMessageBus bus) =>
        _bus = bus;

    public Task Handle(AccountUpdatedDomainEvent notification, CancellationToken ct) =>
        _bus.PublishAsync(
            new AccountUpdatedIntegrationEvent(
                Guid.NewGuid(),
                notification.Account.UpdatedAt,
                (long)notification.Account.Id
            ),
            ct
        );
}
