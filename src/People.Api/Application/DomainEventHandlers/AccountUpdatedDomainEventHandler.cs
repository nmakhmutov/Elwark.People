using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Events;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;

namespace People.Api.Application.DomainEventHandlers
{
    internal sealed class AccountUpdatedDomainEventHandler : INotificationHandler<AccountUpdatedDomainEvent>
    {
        private readonly IKafkaMessageBus _bus;

        public AccountUpdatedDomainEventHandler(IKafkaMessageBus bus) =>
            _bus = bus;

        public Task Handle(AccountUpdatedDomainEvent notification, CancellationToken ct) =>
            _bus.PublishAsync(
                new AccountUpdatedIntegrationEvent(Guid.NewGuid(), notification.UpdatedAt, (long) notification.Id),
                ct
            );
    }
}
