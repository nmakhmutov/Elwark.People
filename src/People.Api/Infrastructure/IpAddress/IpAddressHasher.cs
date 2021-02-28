using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace People.Api.Infrastructure.IpAddress
{
    public sealed class IpAddressHasher : IIpAddressHasher
    {
        private readonly RijndaelManaged _rijndael;

        public IpAddressHasher(string hash)
        {
            _rijndael = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = Encoding.UTF8.GetBytes(hash)
            };
        }

        public byte[] CreateHash(IPAddress ip)
        {
            using var transform = _rijndael.CreateEncryptor();
            var bytes = ip.GetAddressBytes();
            
            return transform.TransformFinalBlock(bytes, 0, bytes.Length);
        }
    }
}