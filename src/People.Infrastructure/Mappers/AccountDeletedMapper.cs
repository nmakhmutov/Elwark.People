using People.Domain.DomainEvents;
using People.Domain.IntegrationEvents;
using People.Infrastructure.Outbox;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Mappers;

public sealed class AccountDeletedMapper : IOutboxEventMapper<AccountDeletedDomainEvent>
{
    public OutboxMessage Map(AccountDeletedDomainEvent evt)
    {
        var payload = new AccountDeletedIntegrationEvent(
            Guid.CreateVersion7(),
            evt.Id,
            evt.OccurredAt
        );

        return OutboxMessage.Create(payload);
    }
}
