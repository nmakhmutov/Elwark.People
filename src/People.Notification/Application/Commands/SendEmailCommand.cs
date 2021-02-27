using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.AggregateModels.EmailProvider;
using People.Infrastructure.Mongo;
using Polly;
using Polly.Retry;

namespace People.Notification.Application.Commands
{
    public sealed record SendEmailCommand(string Email, string Subject, string Body) : IRequest;

    internal sealed class SendEmailCommandHandler : IRequestHandler<SendEmailCommand>
    {
        private readonly IMediator _mediator;
        private readonly IEmailProviderRepository _repository;
        private readonly AsyncRetryPolicy<EmailProviderType?> _policy;

        public SendEmailCommandHandler(IMediator mediator, IEmailProviderRepository repository)
        {
            _mediator = mediator;
            _repository = repository;
            _policy = Policy<EmailProviderType?>
                .Handle<MongoUpdateException>()
                .RetryForeverAsync();
        }

        public async Task<Unit> Handle(SendEmailCommand request, CancellationToken ct)
        {
            var provider = await DequeueProviderAsync(ct);

            return await (provider switch
            {
                EmailProviderType.Sendgrid =>
                    _mediator.Send(new SendEmailBySendgridCommand(request.Email, request.Subject, request.Body), ct),

                EmailProviderType.Gmail =>
                    _mediator.Send(new SendEmailByGmailCommand(request.Email, request.Subject, request.Body), ct),

                null => Task.FromResult(Unit.Value),

                _ => throw new ArgumentOutOfRangeException()
            });
        }

        private Task<EmailProviderType?> DequeueProviderAsync(CancellationToken ct) =>
            _policy.ExecuteAsync(async token =>
                {
                    var provider = await _repository.GetNextAsync(token);
                    if (provider is null)
                        return null;

                    provider.DecreaseBalance();

                    await _repository.UpdateAsync(provider, token);
                    return provider.Id;
                },
                ct
            );
    }
}