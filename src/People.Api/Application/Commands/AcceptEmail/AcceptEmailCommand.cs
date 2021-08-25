using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;

namespace People.Api.Application.Commands.AcceptEmail
{
    public sealed record AcceptEmailCommand(Identity.Email Email, string Subject, string Body) : IRequest;

    internal sealed record AcceptEmailCommandHandler : IRequestHandler<AcceptEmailCommand>
    {
        private readonly IKafkaMessageBus _bus;

        public AcceptEmailCommandHandler(IKafkaMessageBus bus) =>
            _bus = bus;

        public async Task<Unit> Handle(AcceptEmailCommand request, CancellationToken ct)
        {
            var evt = new EmailMessageCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                request.Email.Value,
                request.Subject,
                request.Body
            );
            await _bus.PublishAsync(evt, ct);

            return Unit.Value;
        }
    }
}
