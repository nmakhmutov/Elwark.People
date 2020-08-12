using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.People.Shared.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elwark.People.Background.FunctionalTests
{
    public class ConfirmationByUrlEventTest : ScenarioBase
    {
        public static IEnumerable<object[]> Events =>
            new List<object[]>
            {
                new object[]
                {
                    new ConfirmationByUrlCreatedIntegrationEvent(Email, new Uri("http://localhost"), "en", ConfirmationType.ConfirmIdentity)
                },
                new object[]
                {
                    new ConfirmationByUrlCreatedIntegrationEvent(Email, new Uri("http://localhost"), "en", ConfirmationType.UpdatePassword)
                },
                new object[]
                {
                    new ConfirmationByUrlCreatedIntegrationEvent(Email, new Uri("http://localhost"), "ru", ConfirmationType.ConfirmIdentity)
                },
                new object[]
                {
                    new ConfirmationByUrlCreatedIntegrationEvent(Email, new Uri("http://localhost"), "ru", ConfirmationType.UpdatePassword)
                },
            };
        
        [Theory, MemberData(nameof(Events))]
        public async Task Confirmation_by_code_created_success(ConfirmationByUrlCreatedIntegrationEvent evt)
        {
            using var server = CreateServer();
            var handler = server.Services.GetService<IIntegrationEventHandler<ConfirmationByUrlCreatedIntegrationEvent>>();

            await handler.HandleAsync(evt);
        }
    }
}