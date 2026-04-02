using People.Domain.DomainEvents;
using People.Domain.IntegrationEvents;
using People.Infrastructure.Outbox;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Mappers;

public sealed class AccountCreatedMapper : IOutboxEventMapper<AccountCreatedDomainEvent>
{
    public OutboxMessage Map(AccountCreatedDomainEvent evt)
    {
        var payload = new AccountCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            evt.Account.Id,
            evt.IpAddress.ToString(),
            evt.OccurredAt
        );

        return OutboxMessage.Create(payload);
    }
}
