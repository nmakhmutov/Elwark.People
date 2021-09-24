using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Domain.Events;
using People.Integration.Event;
using People.Kafka;

namespace People.Account.Api.Application.DomainEventHandlers
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
                notification.Account.GetPrimaryEmail().Value,
                notification.IpAddress.ToString(),
                notification.Account.Language.ToString()
            );
            
            return _bus.PublishAsync(evt, ct);
        }
    }
}
