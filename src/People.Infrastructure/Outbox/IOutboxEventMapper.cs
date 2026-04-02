using JetBrains.Annotations;
using People.Domain.Events;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Outbox;

[UsedImplicitly]
public interface IOutboxEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    OutboxMessage Map(TDomainEvent evt);
}
