using People.Account.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Account.Domain
{
    public class AccountBannedException : ElwarkException
    {
        public Ban Ban { get; }
        
        public AccountBannedException(Ban ban) 
            : base(ElwarkExceptionCodes.AccountBanned, ban.Reason)
        {
            Ban = ban;
        }
    }
}
