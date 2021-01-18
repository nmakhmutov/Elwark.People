using People.Domain.AggregateModels.Account;

namespace People.Domain.Exceptions
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