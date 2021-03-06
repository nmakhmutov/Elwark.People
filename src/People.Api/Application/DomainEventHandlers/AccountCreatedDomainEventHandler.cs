using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Events;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;

namespace People.Api.Application.DomainEventHandlers
{
    internal sealed class AccountCreatedDomainEventHandler : INotificationHandler<AccountCreatedDomainEvent>
    {
        private readonly IKafkaMessageBus _bus;

        public AccountCreatedDomainEventHandler(IKafkaMessageBus bus) =>
            _bus = bus;

        public Task Handle(AccountCreatedDomainEvent notification, CancellationToken ct)
        {
            var evt = new AccountCreatedIntegrationEvent(
                Guid.NewGuid(),
                notification.Account.CreatedAt,
                (long) notification.Account.Id,
                notification.Account.GetPrimaryEmail().Address,
                notification.IpAddress.ToString(),
                notification.Account.Language.ToString()
            );
            
            return _bus.PublishAsync(evt, ct);
        }
    }
}
