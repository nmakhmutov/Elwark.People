using System.Collections.Generic;
using System.Diagnostics;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models.Responses
{
    public class SignUpResponse
    {
        [DebuggerStepThrough]
        public SignUpResponse(AccountId accountId, string name,
            IReadOnlyCollection<RegistrationIdentityResponse> identities)
        {
            AccountId = accountId;
            Name = name;
            Identities = identities;
        }

        public AccountId AccountId { get; }

        public string Name { get; }

        public IReadOnlyCollection<RegistrationIdentityResponse> Identities { get; }
    }
}