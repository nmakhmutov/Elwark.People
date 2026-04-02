using People.Domain.DomainEvents;
using People.Domain.IntegrationEvents;
using People.Infrastructure.Outbox;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Mappers;

public sealed class AccountUpdatedMapper : IOutboxEventMapper<AccountUpdatedDomainEvent>
{
    public OutboxMessage Map(AccountUpdatedDomainEvent evt)
    {
        var payload = new AccountUpdatedIntegrationEvent(
            Guid.CreateVersion7(),
            evt.Id,
            evt.OccurredAt
        );

        return OutboxMessage.Create(payload);
    }
}
