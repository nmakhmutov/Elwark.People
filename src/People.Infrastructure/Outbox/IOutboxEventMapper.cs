using Microsoft.EntityFrameworkCore;
using People.Domain.Events;

namespace People.Infrastructure.Outbox;

public interface IOutboxEventMapper<TDbContext>
    where TDbContext : DbContext
{
    OutboxMessage? Map(IDomainEvent domainEvent);
}
