using Elwark.People.Abstractions;

namespace Elwark.People.Api.Infrastructure.Services.Identity
{
    public interface IIdentityService
    {
        AccountId GetAccountId();

        IdentityId GetIdentityId();

        string? GetIdentityName();
    }
}