using System.Collections.Generic;
using System.Diagnostics;
using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.Models
{
    public class SignUpModel
    {
        [DebuggerStepThrough]
        public SignUpModel(AccountId accountId, string name,
            IReadOnlyCollection<IdentityModel> identities)
        {
            AccountId = accountId;
            Name = name;
            Identities = identities;
        }

        public AccountId AccountId { get; }

        public string Name { get; }

        public IReadOnlyCollection<IdentityModel> Identities { get; }
    }
}