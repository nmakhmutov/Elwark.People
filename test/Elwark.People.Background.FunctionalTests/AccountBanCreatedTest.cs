using System.Collections.Generic;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.People.Shared.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elwark.People.Background.FunctionalTests
{
    public class AccountBanCreatedTest : ScenarioBase
    {
        public static IEnumerable<object[]> AccountBanCreatedEvents =>
            new List<object[]>
            {
                new object[]
                {
                    new AccountBanCreatedIntegrationEvent(Id, Email, BanType.Permanent,
                        "Test reason for permanent block", "en")
                },
                new object[]
                {
                    new AccountBanCreatedIntegrationEvent(Id, Email, BanType.Temporarily,
                        "Test reason for temporarily block", "en")
                },
                new object[]
                {
                    new AccountBanCreatedIntegrationEvent(Id, Email, BanType.Permanent,
                        "Тестовая причина для постоянной блокировки", "ru")
                },
                new object[]
                {
                    new AccountBanCreatedIntegrationEvent(Id, Email, BanType.Temporarily,
                        "Тестовая причина для временной блокировки", "ru")
                }
            };

        [Theory, MemberData(nameof(AccountBanCreatedEvents))]
        public async Task Account_ban_created_event_success(AccountBanCreatedIntegrationEvent evt)
        {
            using var server = CreateServer();
            var handler = server.Services.GetService<IIntegrationEventHandler<AccountBanCreatedIntegrationEvent>>();

            await handler.HandleAsync(evt);
        }
    }
}