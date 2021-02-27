using System.Threading.Tasks;
using MediatR;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using People.Notification.Application.Commands;

namespace People.Notification.Application.IntegrationEventHandler
{
    internal sealed class EmailMessageCreatedHandler : IKafkaHandler<EmailMessageCreatedIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public EmailMessageCreatedHandler(IMediator mediator) =>
            _mediator = mediator;

        public Task HandleAsync(EmailMessageCreatedIntegrationEvent message) =>
            _mediator.Send(new SendEmailCommand(message.Email, message.Subject, message.Body));
    }
}