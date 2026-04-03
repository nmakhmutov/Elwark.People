using System.Security.Claims;
using People.Api.Infrastructure;
using Xunit;

namespace Unit.Api.Tests.Infrastructure;

public sealed class ClaimsPrincipalExtensionsTests
{
    /// <summary>
    /// JWT <c>sub</c> is typically mapped to <see cref="ClaimTypes.NameIdentifier"/> at the API layer.
    /// </summary>
    [Fact]
    public void GetAccountId_WithNameIdentifierClaim_ReturnsAccountId()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42")],
                authenticationType: "Test"
            )
        );

        var id = principal.GetAccountId();

        Assert.Equal(42L, (long)id);
    }

    [Fact]
    public void GetAccountId_WithoutNameIdentifierClaim_ThrowsArgumentException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var ex = Assert.Throws<ArgumentException>(() => principal.GetAccountId());

        Assert.Equal("Account id cannot be null in identity service", ex.Message);
    }

    [Fact]
    public void GetAccountId_WithInvalidLong_ThrowsFormatException()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "not-a-long")],
                authenticationType: "Test"));

        Assert.Throws<FormatException>(() => principal.GetAccountId());
    }
}
