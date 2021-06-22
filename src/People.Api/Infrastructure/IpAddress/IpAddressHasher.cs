using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace People.Api.Infrastructure.IpAddress
{
    public sealed class IpAddressHasher : IIpAddressHasher
    {
        private readonly Aes _aes;

        public IpAddressHasher(string hash)
        {
            _aes = Aes.Create();

            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;
            _aes.Key = Encoding.UTF8.GetBytes(hash);
        }

        public byte[] CreateHash(IPAddress ip)
        {
            using var transform = _aes.CreateEncryptor();
            var bytes = ip.GetAddressBytes();

            return transform.TransformFinalBlock(bytes, 0, bytes.Length);
        }
    }
}
