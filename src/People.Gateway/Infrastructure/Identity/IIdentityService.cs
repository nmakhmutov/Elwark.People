using People.Grpc.Common;

namespace People.Gateway.Infrastructure.Identity
{
    public interface IIdentityService
    {
        long GetId();
        
        AccountId GetAccountId();
    }
}
