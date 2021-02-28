using System.Net;

namespace People.Api.Infrastructure.IpAddress
{
    public interface IIpAddressHasher
    {
        byte[] CreateHash(IPAddress ip);
    }
}