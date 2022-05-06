using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.IpAddress;

internal sealed class IpAddressHasher : IIpAddressHasher, IDisposable
{
    private readonly RC2 _rc2;

    public IpAddressHasher(string hash)
    {
        _rc2 = RC2.Create();
        _rc2.Mode = CipherMode.CBC;
        _rc2.Padding = PaddingMode.PKCS7;
        _rc2.Key = Encoding.UTF8.GetBytes(hash);
    }

    public byte[] CreateHash(IPAddress ip)
    {
        using var transform = _rc2.CreateEncryptor();
        var bytes = ip.GetAddressBytes();

        return transform.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    public void Dispose() =>
        _rc2.Dispose();
}
