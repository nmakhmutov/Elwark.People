namespace People.Infrastructure.Outbox;

public enum OutboxStatus
{
    Pending = 1,
    Success = 2,
    Fail = 3
}
