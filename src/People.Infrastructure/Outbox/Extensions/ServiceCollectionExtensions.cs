using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace People.Infrastructure.Outbox.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services,
        Func<OutboxMapperRegistry<TDbContext>, OutboxMapperRegistry<TDbContext>> configure
    ) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configure);

        var registry = configure(new OutboxMapperRegistry<TDbContext>());

        services.AddSingleton(registry);
        services.AddSingleton<OutboxPipeline<TDbContext>>();

        return services;
    }
}
