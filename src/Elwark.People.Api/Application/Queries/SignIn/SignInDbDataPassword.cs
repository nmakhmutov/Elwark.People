using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;

namespace Elwark.People.Api.Application.Queries.SignIn
{
    public class SignInDbDataPassword : SignInDbDataBase<Identification.Email>
    {
        public class PasswordModel
        {
            public PasswordModel(byte[] hash, byte[] salt)
            {
                Hash = hash;
                Salt = salt;
            }

            public byte[] Hash { get; }
        
            public byte[] Salt { get; }
        }
        
        public SignInDbDataPassword(Identification.Email identifier, IdentityId identityId, AccountId accountId,
            bool isConfirmed, BanDetailsResponse? ban, PasswordModel? password)
            : base(identifier, identityId, accountId, isConfirmed, ban) =>
            Password = password;

        public PasswordModel? Password { get; }

        public void Validate(IPasswordHasher hasher, string password)
        {
            base.Validate();

            if (Password is null)
                throw new ElwarkPasswordException(PasswordError.NotSet);

            if (!hasher.IsEqual(password, Password.Hash, Password.Salt))
                throw new ElwarkPasswordException(PasswordError.Mismatch);
        }
    }
}