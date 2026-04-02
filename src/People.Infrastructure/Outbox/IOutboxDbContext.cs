using Microsoft.EntityFrameworkCore;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Outbox;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }

    DbSet<OutboxConsumer> OutboxConsumers { get; }
}
