using System.Net;
using Microsoft.Extensions.Options;
using People.Infrastructure.Cryptography;
using People.Infrastructure;
using Xunit;

namespace People.UnitTests.Infrastructure;

public sealed class IpHasherTests
{
    private static IpHasher CreateHasher() =>
        new(Options.Create(new AppSecurityOptions("test-app-key", "test-app-vector")));

    [Fact]
    public void CreateHash_SameIp_ProducesSameBytes()
    {
        var hasher = CreateHasher();
        var ip = IPAddress.Parse("192.168.1.10");

        var first = hasher.CreateHash(ip);
        var second = hasher.CreateHash(ip);

        Assert.Equal(first, second);
    }

    [Fact]
    public void CreateHash_DifferentIps_ProducesDifferentBytes()
    {
        var hasher = CreateHasher();
        var a = hasher.CreateHash(IPAddress.Parse("10.0.0.1"));
        var b = hasher.CreateHash(IPAddress.Parse("10.0.0.2"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void CreateHash_OutputLength_IsSha256DigestSize()
    {
        var hasher = CreateHasher();
        var hash = hasher.CreateHash(IPAddress.Loopback);

        Assert.Equal(32, hash.Length);
    }
}
