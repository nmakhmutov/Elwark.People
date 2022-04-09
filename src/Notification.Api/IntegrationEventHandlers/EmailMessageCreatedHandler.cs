using System.Net.Mail;
using Common.Kafka;
using Common.Mongo;
using Integration.Event;
using Notification.Api.Infrastructure.Provider;
using Notification.Api.Infrastructure.Provider.Gmail;
using Notification.Api.Infrastructure.Provider.SendGrid;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.Models;
using Polly;
using Polly.Retry;

namespace Notification.Api.IntegrationEventHandlers;

internal sealed class EmailMessageCreatedHandler : IKafkaHandler<EmailMessageCreatedIntegrationEvent>
{
    private readonly ILogger<EmailMessageCreatedHandler> _logger;
    private readonly AsyncRetryPolicy<EmailProvider.Type?> _policy;
    private readonly IPostponedEmailRepository _postponed;
    private readonly IEmailProviderRepository _repository;
    private readonly IEnumerable<IEmailSender> _senders;

    public EmailMessageCreatedHandler(IEmailProviderRepository repository, IEnumerable<IEmailSender> senders,
        ILogger<EmailMessageCreatedHandler> logger, IPostponedEmailRepository postponed)
    {
        _repository = repository;
        _senders = senders;
        _logger = logger;
        _postponed = postponed;
        _policy = Policy<EmailProvider.Type?>
            .Handle<MongoUpdateException>()
            .RetryForeverAsync();
    }

    public async Task HandleAsync(EmailMessageCreatedIntegrationEvent message)
    {
        var type = await _policy.ExecuteAsync(DequeueEmailProviderAsync);

        var provider = _senders.FirstOrDefault(x => type switch
        {
            EmailProvider.Type.Sendgrid => x is SendgridProvider,
            EmailProvider.Type.Gmail => x is GmailProvider,
            null => false,
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Unknown email provider")
        });

        if (provider is null)
        {
            if (message.IsDurable)
                await _postponed.CreateAsync(
                    new PostponedEmail(
                        message.Email,
                        message.Subject,
                        message.Body,
                        DateTime.UtcNow.Date.AddDays(1)
                    )
                );

            return;
        }

        await provider.SendEmailAsync(new MailAddress(message.Email), message.Subject, message.Body);

        _logger.LogInformation("Message for '{C}' sent by the provider {P}", message.Email, type);
    }

    private async Task<EmailProvider.Type?> DequeueEmailProviderAsync()
    {
        var provider = await _repository.GetNextAsync();
        if (provider is null)
            return null;

        provider.DecreaseBalance();

        await _repository.UpdateAsync(provider);
        return provider.Id;
    }
}
