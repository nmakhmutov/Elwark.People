namespace People.Domain;

public static class TimeProviderExtensions
{
    public static DateTime UtcNow(this TimeProvider timeProvider) =>
        timeProvider.GetUtcNow().UtcDateTime;
}
