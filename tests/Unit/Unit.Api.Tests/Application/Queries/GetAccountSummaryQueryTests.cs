using People.Application.Queries.GetAccountSummary;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Application.Queries;

public sealed class GetAccountSummaryQueryTests
{
    [Fact]
    public async Task Handle_RowReturned_MapsToAccountSummary()
    {
        var id = new AccountId(802L);
        var row = new DictionaryNpgsqlRow(new Dictionary<int, object?>
        {
            [0] = (long)id,
            [1] = "sum@example.com",
            [2] = "nick",
            [3] = "First",
            [4] = "Last",
            [5] = false,
            [6] = "https://pic.example/id",
            [7] = "en",
            [8] = "EU",
            [9] = "DE",
            [10] = TimeZoneInfo.Utc.Id,
            [11] = new[] { "member", "admin" },
            [12] = DBNull.Value
        });

        var accessor = new TestNpgsqlAccessor(row);
        var handler = new GetAccountSummaryQueryHandler(accessor);

        var result = await handler.Handle(new GetAccountSummaryQuery(id), CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal("sum@example.com", result.Email);
        Assert.Equal(Nickname.Parse("nick"), result.Name.Nickname);
        Assert.Equal("First", result.Name.FirstName);
        Assert.Equal("Last", result.Name.LastName);
        Assert.False(result.Name.UseNickname);
        Assert.Equal(Picture.Parse("https://pic.example/id"), result.Picture);
        Assert.Equal(Locale.Parse("en"), result.Locale);
        Assert.Equal(RegionCode.Parse("EU"), result.RegionCode);
        Assert.Equal(CountryCode.Parse("DE"), result.CountryCode);
        Assert.Equal(Timezone.Utc, result.Timezone);
        Assert.Equal(new[] { "member", "admin" }, result.Roles);
        Assert.Null(result.Ban);
    }

    [Fact]
    public async Task Handle_NoRow_ThrowsNotFound()
    {
        var accessor = new TestNpgsqlAccessor();
        var handler = new GetAccountSummaryQueryHandler(accessor);

        await Assert.ThrowsAsync<AccountException>(async () =>
            await handler.Handle(new GetAccountSummaryQuery(new AccountId(803L)), CancellationToken.None));
    }
}
