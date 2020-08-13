using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInDbDataBase<T>
        where T : Identification
    {
        public SignInDbDataBase(T identifier, IdentityId identityId, AccountId accountId, bool isConfirmed,
            BanDetail? ban)
        {
            Identifier = identifier;
            IdentityId = identityId;
            AccountId = accountId;
            IsConfirmed = isConfirmed;
            Ban = ban;
        }

        public T Identifier { get; }

        public IdentityId IdentityId { get; }

        public AccountId AccountId { get; }

        public bool IsConfirmed { get; }

        public BanDetail? Ban { get; }

        public virtual void Validate()
        {
            if (!IsConfirmed)
                throw new ElwarkIdentificationException(IdentificationError.NotConfirmed, Identifier);

            if (Ban is {})
                throw new ElwarkAccountBlockedException(AccountId, Ban.Type, Ban.ExpiredAt, Ban.Reason);
        }
    }
}