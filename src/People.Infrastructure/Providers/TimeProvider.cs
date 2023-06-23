using People.Domain.SeedWork;

namespace People.Infrastructure.Providers;

internal sealed class TimeProvider : ITimeProvider
{
    public DateTime Now =>
        DateTime.UtcNow;
}
