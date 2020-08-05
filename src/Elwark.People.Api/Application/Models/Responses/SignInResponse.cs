using System.Diagnostics;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models.Responses
{
    public class SignInResponse
    {
        [DebuggerStepThrough]
        public SignInResponse(AccountId accountId, IdentityId identityId)
        {
            AccountId = accountId;
            IdentityId = identityId;
        }

        public AccountId AccountId { get; }

        public IdentityId IdentityId { get; }
    }
}