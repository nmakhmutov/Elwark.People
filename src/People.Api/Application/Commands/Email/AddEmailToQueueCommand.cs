using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;

namespace People.Api.Application.Commands.Email
{
    public sealed record AddEmailToQueueCommand(string Email, string Subject, string Body) : IRequest;

    internal sealed record AddEmailToQueueCommandHandler : IRequestHandler<AddEmailToQueueCommand>
    {
        private readonly IKafkaMessageBus _bus;

        public AddEmailToQueueCommandHandler(IKafkaMessageBus bus) =>
            _bus = bus;

        public async Task<Unit> Handle(AddEmailToQueueCommand request, CancellationToken ct)
        {
            var evt = new EmailMessageCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                request.Email,
                request.Subject,
                request.Body
            );
            await _bus.PublishAsync(evt, ct);

            return Unit.Value;
        }
    }
}
