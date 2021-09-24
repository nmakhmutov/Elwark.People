using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using People.Grpc.Notification;
using People.Integration.Event;
using People.Kafka;

namespace People.Notification.Api.Grpc
{
    public class NotificationService : People.Grpc.Notification.NotificationService.NotificationServiceBase
    {
        private readonly IKafkaMessageBus _bus;
        
        public NotificationService(IKafkaMessageBus bus) =>
            _bus = bus;

        public override async Task<Empty> SendEmail(SendEmailRequest request, ServerCallContext context)
        {
            var evt = new EmailMessageCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                request.Email,
                request.Subject,
                request.Body
            );
            await _bus.PublishAsync(evt, context.CancellationToken);

            return new Empty();
        }
    }
}
