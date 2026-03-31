using Microsoft.EntityFrameworkCore;

namespace People.Infrastructure.Outbox;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
