using System.Net.Mail;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Commands.SendEmail;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;

namespace People.Api.Application.IntegrationEventHandlers
{
    internal sealed class EmailMessageCreatedHandler : IKafkaHandler<EmailMessageCreatedIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public EmailMessageCreatedHandler(IMediator mediator) =>
            _mediator = mediator;

        public Task HandleAsync(EmailMessageCreatedIntegrationEvent message) =>
            _mediator.Send(new SendEmailCommand(new MailAddress(message.Email), message.Subject, message.Body));
    }
}
