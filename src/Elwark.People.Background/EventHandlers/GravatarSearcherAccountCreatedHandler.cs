using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Background.Services.Gravatar;
using Elwark.People.Shared.IntegrationEvents;

namespace Elwark.People.Background.EventHandlers
{
    public class GravatarSearcherAccountCreatedHandler : IIntegrationEventHandler<AccountCreatedIntegrationEvent>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly IGravatarService _gravatar;

        public GravatarSearcherAccountCreatedHandler(IGravatarService gravatar,
            IIntegrationEventPublisher eventPublisher)
        {
            _gravatar = gravatar;
            _eventPublisher = eventPublisher;
        }

        public async Task HandleAsync(AccountCreatedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            var profile = await _gravatar.GetAsync(evt.PrimaryEmail);
            if (profile is null)
                return;

            var integrationEvent = new MergeAccountInformationIntegrationEvent
            {
                AccountId = evt.AccountId,
                Picture = new Uri(profile.ThumbnailUrl, "?s=500"),
                FirstName = profile.Name?.FirstName,
                LastName = profile.Name?.LastName,
                Bio = profile.AboutMe
            };

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
        }
    }
}