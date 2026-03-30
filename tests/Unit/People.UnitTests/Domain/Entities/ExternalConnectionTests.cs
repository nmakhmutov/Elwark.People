using System.Reflection;
using People.Domain.Entities;
using Xunit;

namespace People.UnitTests.Domain.Entities;

public sealed class ExternalConnectionTests
{
    private static DateTime GetCreatedAt(ExternalConnection connection) =>
        (DateTime)typeof(ExternalConnection).GetField("_createdAt", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(connection)!;

    [Fact]
    public void Google_SetsTypeAndFields()
    {
        var now = new DateTime(2026, 4, 1, 15, 30, 0, DateTimeKind.Utc);

        var c = ExternalConnection.Google("gid-1", "Ann", "Bee", now);

        Assert.Equal(ExternalService.Google, c.Type);
        Assert.Equal("gid-1", c.Identity);
        Assert.Equal("Ann", c.FirstName);
        Assert.Equal("Bee", c.LastName);
        Assert.Equal(now, GetCreatedAt(c));
    }

    [Fact]
    public void Microsoft_SetsTypeAndFields()
    {
        var now = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc);

        var c = ExternalConnection.Microsoft("ms-9", null, "Zed", now);

        Assert.Equal(ExternalService.Microsoft, c.Type);
        Assert.Equal("ms-9", c.Identity);
        Assert.Null(c.FirstName);
        Assert.Equal("Zed", c.LastName);
        Assert.Equal(now, GetCreatedAt(c));
    }
}
