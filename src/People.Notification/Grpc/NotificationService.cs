using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using People.Grpc.Notification;
using People.Notification.Application.Commands;

namespace People.Notification.Grpc
{
    public class NotificationService : People.Grpc.Notification.Notification.NotificationBase
    {
        private readonly IMediator _mediator;

        public NotificationService(IMediator mediator) =>
            _mediator = mediator;

        public override async Task<Empty> SendEmail(SendEmailRequest request, ServerCallContext context)
        {
            await _mediator.Send(
                new AddEmailToQueueCommand(request.Email, request.Subject, request.Body),
                context.CancellationToken
            );

            return new Empty();
        }
    }
}