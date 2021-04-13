using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure.Provider.Email;
using People.Api.Infrastructure.Provider.Email.Gmail;
using People.Api.Infrastructure.Provider.Email.SendGrid;
using People.Domain.AggregateModels.EmailProvider;
using People.Infrastructure.Mongo;
using Polly;
using Polly.Retry;

namespace People.Api.Application.Commands.Email
{
    public sealed record SendEmailCommand(MailAddress Email, string Subject, string Body) : IRequest;

    internal sealed class SendEmailCommandHandler : IRequestHandler<SendEmailCommand>
    {
        private readonly IEnumerable<IEmailSender> _senders;
        private readonly IEmailProviderRepository _repository;
        private readonly AsyncRetryPolicy<EmailProviderType?> _policy;

        public SendEmailCommandHandler(IEmailProviderRepository repository, IEnumerable<IEmailSender> senders)
        {
            _repository = repository;
            _senders = senders;
            _policy = Policy<EmailProviderType?>
                .Handle<MongoUpdateException>()
                .RetryForeverAsync();
        }

        public async Task<Unit> Handle(SendEmailCommand request, CancellationToken ct)
        {
            var type = await _policy.ExecuteAsync(DequeueEmailProviderAsync, ct);

            var provider = _senders.FirstOrDefault(x => type switch
            {
                EmailProviderType.Sendgrid => x is SendgridProvider,
                EmailProviderType.Gmail => x is GmailProvider,
                null => false,
                _ => throw new ArgumentOutOfRangeException(nameof(type), "Unknown email provider")
            });

            if (provider is null)
                return Unit.Value;

            await provider.SendEmailAsync(request.Email, request.Subject, request.Body, ct);

            return Unit.Value;
        }

        private async Task<EmailProviderType?> DequeueEmailProviderAsync(CancellationToken ct)
        {
            var provider = await _repository.GetNextAsync(ct);
            if (provider is null)
                return null;

            provider.DecreaseBalance();

            await _repository.UpdateAsync(provider, ct);
            return provider.Id;
        }
    }
}
