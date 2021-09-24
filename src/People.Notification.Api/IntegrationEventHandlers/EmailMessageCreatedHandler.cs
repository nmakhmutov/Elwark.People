using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using People.Integration.Event;
using People.Kafka;
using People.Mongo;
using People.Notification.Api.Infrastructure.Provider;
using People.Notification.Api.Infrastructure.Provider.Gmail;
using People.Notification.Api.Infrastructure.Provider.SendGrid;
using People.Notification.Api.Infrastructure.Repositories;
using People.Notification.Api.Models;
using Polly;
using Polly.Retry;

namespace People.Notification.Api.IntegrationEventHandlers
{
    internal sealed class EmailMessageCreatedHandler : IKafkaHandler<EmailMessageCreatedIntegrationEvent>
    {
        private readonly AsyncRetryPolicy<EmailProvider.Type?> _policy;
        private readonly IEmailProviderRepository _repository;
        private readonly IEnumerable<IEmailSender> _senders;

        public EmailMessageCreatedHandler(IEmailProviderRepository repository, IEnumerable<IEmailSender> senders)
        {
            _repository = repository;
            _senders = senders;
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
                return;

            await provider.SendEmailAsync(new MailAddress(message.Email), message.Subject, message.Body);
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
}
