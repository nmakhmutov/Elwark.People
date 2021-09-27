using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using People.Grpc.Notification;
using People.Integration.Event;
using People.Kafka;
using People.Notification.Api.Infrastructure.Repositories;
using People.Notification.Api.Models;

namespace People.Notification.Api.Grpc
{
    public class NotificationService : People.Grpc.Notification.NotificationService.NotificationServiceBase
    {
        private readonly IKafkaMessageBus _bus;
        private readonly IPostponedEmailRepository _postponed;

        public NotificationService(IKafkaMessageBus bus, IPostponedEmailRepository postponed)
        {
            _bus = bus;
            _postponed = postponed;
        }

        public override async Task<Empty> SendEmail(SendEmailRequest request, ServerCallContext context)
        {
            if (request.IsNow)
            {
                await SendEmailAsync(request.Email, request.Subject, request.Body, context.CancellationToken);

                return new Empty();
            }

            var delay = CalcDelay(TimeZoneInfo.FindSystemTimeZoneById(request.UserTimeZone));
            if (!delay.HasValue)
            {
                await SendEmailAsync(request.Email, request.Subject, request.Body, context.CancellationToken);

                return new Empty();
            }

            await _postponed.CreateAsync(new PostponedEmail(request.Email, request.Subject, request.Body, delay.Value));

            return new Empty();
        }

        private Task SendEmailAsync(string email, string subject, string body, CancellationToken ct) =>
            _bus.PublishAsync(EmailMessageCreatedIntegrationEvent.CreateDurable(email, subject, body), ct);

        private static DateTime? CalcDelay(TimeZoneInfo timezone)
        {
            var now = DateTime.UtcNow;
            var local = TimeZoneInfo.ConvertTimeFromUtc(now, timezone);
            if (local.Hour is >= 9 and < 21)
                return null;

            var date = local.Hour < 9
                ? local
                : local.AddDays(1);

            return TimeZoneInfo.ConvertTimeToUtc(
                new DateTime(date.Year, date.Month, date.Day, 9, 0, 0, DateTimeKind.Unspecified),
                timezone
            );
        }
    }
}
