// using System.Threading;
// using System.Threading.Tasks;
// using Elwark.EventBus;
// using Elwark.People.Abstractions;
// using Elwark.People.Shared.IntegrationEvents;
// using Microsoft.Extensions.Logging;
//
// namespace Elwark.People.Background.Services
// {
//     public interface IEmailSendService
//     {
//         Task SendAsync(string email, string subject, string body, CancellationToken ct = default);
//     }
//
//     public class EmailSendService : IEmailSendService
//     {
//         private readonly IIntegrationEventPublisher _eventPublisher;
//         private readonly ILogger<EmailSendService> _logger;
//
//         public EmailSendService(IIntegrationEventPublisher eventPublisher, ILogger<EmailSendService> logger)
//         {
//             _eventPublisher = eventPublisher;
//             _logger = logger;
//         }
//
//         public Task SendAsync(string email, string subject, string body, CancellationToken ct)
//         {
//             _logger.LogInformation("Sending email message for {0} with subject {1}", email, subject);
//             return _eventPublisher.PublishAsync(new EmailCreatedIntegrationEvent(email.Value, subject, body), ct);
//         }
//     }
// }


