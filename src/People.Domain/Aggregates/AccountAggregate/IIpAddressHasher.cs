using System.Net;

namespace People.Domain.Aggregates.AccountAggregate
{
    public interface IIpAddressHasher
    {
        byte[] CreateHash(IPAddress ip);
    }
}
