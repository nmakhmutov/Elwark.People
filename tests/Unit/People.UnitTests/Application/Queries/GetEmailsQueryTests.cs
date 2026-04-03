using People.Application.Queries.GetEmails;
using People.Domain.Entities;
using Xunit;

namespace People.UnitTests.Application.Queries;

public sealed class GetEmailsQueryTests
{
    [Fact]
    public async Task Handle_ReturnsMappedUserEmails()
    {
        var id = new AccountId(804L);
        var rowA = new DictionaryNpgsqlRow(new Dictionary<int, object?>
        {
            [0] = "a@example.com",
            [1] = true,
            [2] = true
        });
        var rowB = new DictionaryNpgsqlRow(new Dictionary<int, object?>
        {
            [0] = "b@example.com",
            [1] = false,
            [2] = false
        });

        var accessor = new TestNpgsqlAccessor(rowA, rowB);
        var handler = new GetEmailsQueryHandler(accessor);

        var result = await handler.Handle(new GetEmailsQuery(id), CancellationToken.None);

        Assert.Equal(2, result.Count);
        var list = result.ToList();
        Assert.Contains(list, e => e is { Email: "a@example.com", IsPrimary: true, IsConfirmed: true });
        Assert.Contains(list, e => e is { Email: "b@example.com", IsPrimary: false, IsConfirmed: false });
    }

    [Fact]
    public async Task Handle_NoRows_ReturnsEmptyCollection()
    {
        var accessor = new TestNpgsqlAccessor();
        var handler = new GetEmailsQueryHandler(accessor);

        var result = await handler.Handle(new GetEmailsQuery(new AccountId(805L)), CancellationToken.None);

        Assert.Empty(result);
    }
}
