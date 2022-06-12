using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using People.Domain.SeedWork;

namespace People.Infrastructure.Cryptography;

internal sealed class IpHasher : IIpHasher
{
    private readonly AppSecurityOptions _options;

    public IpHasher(IOptions<AppSecurityOptions> options) =>
        _options = options.Value;

    public byte[] CreateHash(IPAddress ip)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(ip.GetAddressBytes().Concat(_options.AppKey).ToArray());
    }
}
