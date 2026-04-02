namespace People.Infrastructure.Outbox.Entities;

public enum OutboxStatus
{
    Unknown = 0,
    Created = 1,
    Pending = 2,
    Completed = 3,
    Failed = 4
}
