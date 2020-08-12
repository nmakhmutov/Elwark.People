using System.Collections.Generic;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Shared.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elwark.People.Background.FunctionalTests
{
    public class AccountBanRemovedTest : ScenarioBase
    {
        public static IEnumerable<object[]> AccountBanRemovedEvents =>
            new List<object[]>
            {
                new object[]
                {
                    new AccountBanRemovedIntegrationEvent(Id, Email, "en")
                },
                new object[]
                {
                    new AccountBanRemovedIntegrationEvent(Id, Email, "ru")
                }
            };

        [Theory, MemberData(nameof(AccountBanRemovedEvents))]
        public async Task Account_ban_removed_event_success(AccountBanRemovedIntegrationEvent evt)
        {
            using var server = CreateServer();
            var handler = server.Services.GetService<IIntegrationEventHandler<AccountBanRemovedIntegrationEvent>>();

            await handler.HandleAsync(evt);
        }
    }
}