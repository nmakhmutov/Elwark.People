using System.Net;

namespace People.Account.Domain.Aggregates.AccountAggregate
{
    public interface IIpAddressHasher
    {
        byte[] CreateHash(IPAddress ip);
    }
}
