using Elwark.People.Abstractions;
using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkAccountException : ElwarkException
    {
        public ElwarkAccountException(AccountError code, AccountId accountId)
            : base(nameof(AccountError), code.ToString("G"))
        {
            Code = code;
            AccountId = accountId;
        }

        public AccountId AccountId { get; }

        public AccountError Code { get; }

        public static ElwarkAccountException NotFound(AccountId accountId) =>
            new ElwarkAccountException(AccountError.NotFound, accountId);
    }
}