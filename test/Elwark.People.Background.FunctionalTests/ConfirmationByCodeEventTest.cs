using System.Collections.Generic;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.People.Shared.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elwark.People.Background.FunctionalTests
{
    public class ConfirmationByCodeEventTest : ScenarioBase
    {
        public static IEnumerable<object[]> Events =>
            new List<object[]>
            {
                new object[]
                {
                    new ConfirmationByCodeCreatedIntegrationEvent(Email, 99999, "en", ConfirmationType.ConfirmIdentity)
                },
                new object[]
                {
                    new ConfirmationByCodeCreatedIntegrationEvent(Email, 99999, "en", ConfirmationType.UpdatePassword)
                },
                new object[]
                {
                    new ConfirmationByCodeCreatedIntegrationEvent(Email, 99999, "ru", ConfirmationType.ConfirmIdentity)
                },
                new object[]
                {
                    new ConfirmationByCodeCreatedIntegrationEvent(Email, 99999, "ru", ConfirmationType.UpdatePassword)
                },
            };
        
        [Theory, MemberData(nameof(Events))]
        public async Task Confirmation_by_code_created_success(ConfirmationByCodeCreatedIntegrationEvent evt)
        {
            using var server = CreateServer();
            var handler = server.Services.GetService<IIntegrationEventHandler<ConfirmationByCodeCreatedIntegrationEvent>>();

            await handler.HandleAsync(evt);
        }
    }
}