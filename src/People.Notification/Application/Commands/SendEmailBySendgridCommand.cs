using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Notification.Services;

namespace People.Notification.Application.Commands
{
    public sealed record SendEmailBySendgridCommand(string Email, string Subject, string Body) : IRequest;

    internal sealed class SendEmailBySendgridCommandHandler : IRequestHandler<SendEmailBySendgridCommand>
    {
        private readonly ISendgridProvider _sendgrid;

        public SendEmailBySendgridCommandHandler(ISendgridProvider sendgrid) =>
            _sendgrid = sendgrid;

        public async Task<Unit> Handle(SendEmailBySendgridCommand request, CancellationToken ct)
        {
            await _sendgrid.SendEmailAsync(request.Email, request.Subject, request.Body);

            return Unit.Value;
        }
    }
}