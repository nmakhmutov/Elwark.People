using System.Globalization;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Domain.ValueObjects;

public sealed class LocaleTests
{
    [Fact]
    public void Parse_RoundTripsValue()
    {
        var locale = Locale.Parse("en");

        Assert.Equal("en", locale.Value);
        Assert.Equal("en", locale.ToString());
    }

    [Fact]
    public void FromCulture_UsesCultureName()
    {
        var locale = Locale.FromCulture(new CultureInfo("en-US"));

        Assert.Equal("en-US", locale.Value);
        Assert.Equal("en", locale.Language);
    }

    [Fact]
    public void Equality_UsesValue()
    {
        var first = Locale.Parse("de");
        var second = Locale.Parse("de");
        var third = Locale.Parse("fr");

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }
}
