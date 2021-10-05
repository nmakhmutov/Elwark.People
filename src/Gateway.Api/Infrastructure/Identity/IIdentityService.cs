using People.Grpc.Common;

namespace Gateway.Api.Infrastructure.Identity;

public interface IIdentityService
{
    long GetId();

    AccountId GetAccountId();
}
