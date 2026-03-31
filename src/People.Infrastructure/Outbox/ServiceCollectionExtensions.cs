using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using People.Infrastructure.Outbox.EntityFrameworkCore;

namespace People.Infrastructure.Outbox;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services,
        Func<OutboxRegistrationBuilder<TDbContext>, OutboxRegistrationBuilder<TDbContext>> configure
    ) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddTransient<OutboxSaveChangesPipeline<TDbContext>>();
        configure(new OutboxRegistrationBuilder<TDbContext>(services));

        return services;
    }
}
