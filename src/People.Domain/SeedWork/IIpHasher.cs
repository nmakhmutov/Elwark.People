using System.Net;

namespace People.Domain.SeedWork;

public interface IIpHasher
{
    byte[] CreateHash(IPAddress ip);
}
