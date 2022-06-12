using MediatR;
using People.Domain.SeedWork;

namespace People.Infrastructure;

internal static class MediatorExtension
{
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, PeopleDbContext ctx)
    {
        var entities = ctx.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Count > 0);

        foreach (var entity in entities)
        {
            foreach (var notification in entity.Entity.DomainEvents)
                await mediator.Publish(notification);

            entity.Entity.ClearDomainEvents();
        }
    }
}
