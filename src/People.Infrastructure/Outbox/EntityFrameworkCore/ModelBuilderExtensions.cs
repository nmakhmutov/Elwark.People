using Microsoft.EntityFrameworkCore;

namespace People.Infrastructure.Outbox.EntityFrameworkCore;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddOutboxMessageEntity(
        this ModelBuilder modelBuilder,
        string tableName = "outbox_messages",
        string? schema = null
    )
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration(tableName, schema));
        return modelBuilder;
    }
}
