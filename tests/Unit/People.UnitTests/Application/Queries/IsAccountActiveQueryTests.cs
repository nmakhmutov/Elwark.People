using NSubstitute;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Queries.IsAccountActive;
using People.Domain.Entities;
using People.Kafka.Integration;
using Xunit;

namespace People.UnitTests.Application.Queries;

public sealed class IsAccountActiveQueryTests
{
    private static DictionaryNpgsqlRow Row(bool activated, bool banned) =>
        new(new Dictionary<int, object?>
        {
            [0] = activated,
            [1] = banned
        });

    [Fact]
    public async Task Handle_ActivatedAndNotBanned_ReturnsTrueAndPublishesInspected()
    {
        var id = new AccountId(806L);
        var accessor = new TestNpgsqlAccessor(Row(activated: true, banned: false));
        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new IsAccountActiveQueryHandler(bus, accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.True(active);
        await bus.Received(1).PublishAsync(
            Arg.Is<AccountActivity.InspectedIntegrationEvent>(e => e.AccountId == (long)id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotActivated_ReturnsFalseAndStillPublishesInspectedWhenRowExists()
    {
        var id = new AccountId(807L);
        var accessor = new TestNpgsqlAccessor(Row(activated: false, banned: false));
        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new IsAccountActiveQueryHandler(bus, accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
        await bus.Received(1).PublishAsync(
            Arg.Any<AccountActivity.InspectedIntegrationEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Banned_ReturnsFalseAndPublishesInspectedWhenRowExists()
    {
        var id = new AccountId(808L);
        var accessor = new TestNpgsqlAccessor(Row(activated: true, banned: true));
        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new IsAccountActiveQueryHandler(bus, accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
        await bus.Received(1).PublishAsync(
            Arg.Any<AccountActivity.InspectedIntegrationEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoRow_ReturnsFalseWithoutPublishing()
    {
        var id = new AccountId(809L);
        var accessor = new TestNpgsqlAccessor();
        var bus = Substitute.For<IIntegrationEventBus>();

        var handler = new IsAccountActiveQueryHandler(bus, accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
        await bus.DidNotReceive()
            .PublishAsync(Arg.Any<AccountActivity.InspectedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
