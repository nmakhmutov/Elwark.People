using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Domain.ValueObjects;

public sealed class NicknameGeneratorTests
{
    [Fact]
    public void Create_ReturnsValidNickname()
    {
        var nickname = NicknameGenerator.Create();
        var value = nickname.ToString();

        Assert.False(string.IsNullOrWhiteSpace(value));
        Assert.Contains('_', value);
        Assert.InRange(value.Length, 1, Nickname.MaxLength);
    }

    [Fact]
    public void Create_ManyTimes_DoesNotThrow()
    {
        for (var i = 0; i < 1_000; i++)
            _ = NicknameGenerator.Create();
    }
}
