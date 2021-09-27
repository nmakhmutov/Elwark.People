using System.Threading.Tasks;
using People.Integration.Event;
using People.Kafka;
using People.Notification.Api.Infrastructure.Repositories;
using Quartz;

namespace People.Notification.Api.Job
{
    [DisallowConcurrentExecution]
    public sealed class PostponedEmailJob : IJob
    {
        private readonly IPostponedEmailRepository _repository;
        private readonly IKafkaMessageBus _bus;

        public PostponedEmailJob(IPostponedEmailRepository repository, IKafkaMessageBus bus)
        {
            _repository = repository;
            _bus = bus;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await foreach (var item in _repository.GetAsync(context.FireTimeUtc.UtcDateTime)
                .WithCancellation(context.CancellationToken))
            {
                await _bus.PublishAsync(
                    EmailMessageCreatedIntegrationEvent.CreateDurable(item.Email, item.Subject, item.Body),
                    context.CancellationToken
                );

                await _repository.DeleteAsync(item.Id, context.CancellationToken);
            }
        }
    }
}
