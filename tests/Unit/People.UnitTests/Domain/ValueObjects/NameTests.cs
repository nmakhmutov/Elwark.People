using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Domain.ValueObjects;

public sealed class NameTests
{
    [Fact]
    public void Create_ValidNickname_ReturnsInstance()
    {
        var name = Name.Create(Nickname.Parse("nick"));

        Assert.Equal(Nickname.Parse("nick"), name.Nickname);
        Assert.True(name.PreferNickname);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyNickname_ThrowsArgument(string nickname) =>
        Assert.Throws<ArgumentException>(() => Name.Create(Nickname.Parse(nickname)));

    [Fact]
    public void Create_NicknameTooLong_ThrowsOutOfRange()
    {
        var longNick = new string('x', Nickname.MaxLength + 1);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Name.Create(Nickname.Parse(longNick)));
        Assert.Equal("nickname", ex.ParamName);
    }

    [Fact]
    public void Create_FirstNameTooLong_ThrowsOutOfRange()
    {
        var longFirst = new string('f', Name.FirstNameLength + 1);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Name.Create(Nickname.Parse("ok"), longFirst));
        Assert.Equal("firstName", ex.ParamName);
    }

    [Fact]
    public void Create_LastNameTooLong_ThrowsOutOfRange()
    {
        var longLast = new string('l', Name.LastNameLength + 1);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Name.Create(Nickname.Parse("ok"), null, longLast));
        Assert.Equal("lastName", ex.ParamName);
    }

    [Fact]
    public void FullName_PreferNickname_ReturnsNickname()
    {
        var name = Name.Create(Nickname.Parse("nick"), "John", "Doe", preferNickname: true);
        Assert.Equal("nick", name.FullName());
    }

    [Fact]
    public void FullName_RealName_ReturnsFirstLast()
    {
        var name = Name.Create(Nickname.Parse("nick"), "John", "Doe", preferNickname: false);
        Assert.Equal("John Doe", name.FullName());
    }

    [Fact]
    public void FullName_NoFirstLast_ReturnsNickname()
    {
        var name = Name.Create(Nickname.Parse("nick"), preferNickname: false);
        Assert.Equal("nick", name.FullName());
    }

    [Fact]
    public void Equals_SameValues_True()
    {
        var a = Name.Create(Nickname.Parse("n"), "F", "L", false);
        var b = Name.Create(Nickname.Parse("n"), "F", "L", false);
        Assert.True(a == b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentNicknames_False()
    {
        var a = Name.Create(Nickname.Parse("n1"));
        var b = Name.Create(Nickname.Parse("n2"));
        Assert.True(a != b);
    }
}
