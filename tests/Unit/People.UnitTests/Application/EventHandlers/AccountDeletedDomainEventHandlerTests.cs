using NSubstitute;
using People.Api.Application.DomainEventHandlers;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.DomainEvents;
using People.Domain.Entities;
using People.Kafka.Integration;
using Xunit;

namespace People.UnitTests.Application.EventHandlers;

public sealed class AccountDeletedDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_PublishesAccountDeletedIntegrationEvent_WithAccountId()
    {
        var id = new AccountId(77L);
        var bus = Substitute.For<IIntegrationEventBus>();

        var sut = new AccountDeletedDomainEventHandler(bus);
        await sut.Handle(new AccountDeletedDomainEvent(id), CancellationToken.None);

        await bus.Received(1).PublishAsync(
            Arg.Is<AccountDeletedIntegrationEvent>(e => e.AccountId == 77L),
            Arg.Any<CancellationToken>());
    }
}
