using People.Application.Queries.GetAccountSummary;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;
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
            [11] = "yyyy-MM-dd",
            [12] = "HH:mm",
            [13] = (int)DayOfWeek.Wednesday,
            [14] = new[] { "member", "admin" }
        });

        var accessor = new TestNpgsqlAccessor(row);
        var handler = new GetAccountSummaryQueryHandler(accessor);

        var result = await handler.Handle(new GetAccountSummaryQuery(id), CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal("sum@example.com", result.Email);
        Assert.Equal(Nickname.Parse("nick"), result.Name.Nickname);
        Assert.Equal("First", result.Name.FirstName);
        Assert.Equal("Last", result.Name.LastName);
        Assert.False(result.Name.PreferNickname);
        Assert.Equal(Picture.Parse("https://pic.example/id"), result.Picture);
        Assert.Equal(Language.Parse("en"), result.Language);
        Assert.Equal(RegionCode.Parse("EU"), result.RegionCode);
        Assert.Equal(CountryCode.Parse("DE"), result.CountryCode);
        Assert.Equal(TimeZone.Utc, result.TimeZone);
        Assert.Equal(DateFormat.Parse("yyyy-MM-dd"), result.DateFormat);
        Assert.Equal(TimeFormat.Parse("HH:mm"), result.TimeFormat);
        Assert.Equal(DayOfWeek.Wednesday, result.StartOfWeek);
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
