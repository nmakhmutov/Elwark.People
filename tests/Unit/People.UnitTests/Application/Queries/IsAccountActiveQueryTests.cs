using People.Application.Queries.IsAccountActive;
using People.Domain.Entities;
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
    public async Task Handle_ActivatedAndNotBanned_ReturnsTrue()
    {
        var id = new AccountId(806L);
        var accessor = new TestNpgsqlAccessor(Row(activated: true, banned: false));

        var handler = new IsAccountActiveQueryHandler(accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.True(active);
    }

    [Fact]
    public async Task Handle_NotActivated_ReturnsFalse()
    {
        var id = new AccountId(807L);
        var accessor = new TestNpgsqlAccessor(Row(activated: false, banned: false));

        var handler = new IsAccountActiveQueryHandler(accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
    }

    [Fact]
    public async Task Handle_Banned_ReturnsFalse()
    {
        var id = new AccountId(808L);
        var accessor = new TestNpgsqlAccessor(Row(activated: true, banned: true));

        var handler = new IsAccountActiveQueryHandler(accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
    }

    [Fact]
    public async Task Handle_NoRow_ReturnsFalse()
    {
        var id = new AccountId(809L);
        var accessor = new TestNpgsqlAccessor();

        var handler = new IsAccountActiveQueryHandler(accessor);

        var active = await handler.Handle(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
    }
}
