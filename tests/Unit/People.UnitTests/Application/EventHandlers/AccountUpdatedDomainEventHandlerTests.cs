using NSubstitute;
using People.Api.Application.DomainEventHandlers;
using People.Api.Application.IntegrationEvents.Events;
using People.Domain.DomainEvents;
using People.Domain.Entities;
using People.Kafka.Integration;
using Xunit;

namespace People.UnitTests.Application.EventHandlers;

public sealed class AccountUpdatedDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_PublishesAccountUpdatedIntegrationEvent_WithAccountId()
    {
        var id = new AccountId(402L);
        var utc = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc);
        var bus = Substitute.For<IIntegrationEventBus>();

        var sut = new AccountUpdatedDomainEventHandler(bus);
        await sut.Handle(new AccountUpdatedDomainEvent(id, utc), CancellationToken.None);

        await bus.Received(1).PublishAsync(
            Arg.Is<AccountUpdatedIntegrationEvent>(e => e.AccountId == 402L),
            Arg.Any<CancellationToken>());
    }
}
