using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using People.Domain.Aggregates.EmailProvider;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;

namespace People.Api.Application.IntegrationEventHandlers
{
    internal sealed class UpdateExpiredProviderEventHandler : IKafkaHandler<ProviderExpiredIntegrationEvent>
    {
        private readonly IEmailProviderRepository _repository;
        private readonly ILogger<UpdateExpiredProviderEventHandler> _logger;

        public UpdateExpiredProviderEventHandler(IEmailProviderRepository repository,
            ILogger<UpdateExpiredProviderEventHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task HandleAsync(ProviderExpiredIntegrationEvent message)
        {
            var provider = await _repository.GetAsync(message.Type);
            if (provider is null)
                return;

            provider.UpdateBalance();

            await _repository.UpdateAsync(provider);
            _logger.LogInformation("Provider '{P}' updated. New balance '{B}'", provider.Id, provider.Balance);
        }
    }
}
