using System.Diagnostics;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models
{
    public class SignInModel
    {
        [DebuggerStepThrough]
        public SignInModel(AccountId accountId, IdentityId identityId)
        {
            AccountId = accountId;
            IdentityId = identityId;
        }

        public AccountId AccountId { get; }

        public IdentityId IdentityId { get; }
    }
}