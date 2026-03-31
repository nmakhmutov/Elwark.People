using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace People.Infrastructure.Outbox;

public sealed class OutboxRegistrationBuilder<TDbContext>(IServiceCollection services)
    where TDbContext : DbContext
{
    public OutboxRegistrationBuilder<TDbContext> AddMapper<TMapper>()
        where TMapper : class, IOutboxEventMapper<TDbContext>
    {
        services.AddTransient<IOutboxEventMapper<TDbContext>, TMapper>();
        return this;
    }
}
