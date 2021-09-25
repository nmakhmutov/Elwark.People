using System.Threading.Tasks;
using MediatR;
using People.Account.Domain.Seed;

namespace People.Account.Api.Infrastructure
{
    public static class MediatorExtension
    {
        public static async ValueTask DispatchDomainEventsAsync(this IMediator mediator, Entity entity)
        {
            if (entity.DomainEvents.Count == 0)
                return;

            foreach (var evt in entity.DomainEvents)
                await mediator.Publish(evt);

            entity.ClearDomainEvents();
        }
    }
}
