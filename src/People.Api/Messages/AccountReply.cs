using Google.Protobuf.WellKnownTypes;
using People.Application.Queries.GetAccountSummary;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class AccountReply
{
    internal static AccountReply Map(AccountSummary account) =>
        new()
        {
            Id = account.Id,
            Email = account.Email,
            Nickname = account.Name.Nickname.ToString(),
            FirstName = account.Name.FirstName,
            LastName = account.Name.LastName,
            FullName = account.Name.FullName(),
            Picture = account.Picture.ToString(),
            CountryCode = account.CountryCode.ToString(),
            TimeZone = account.Timezone.ToString(),
            Language = Language.Create(account.Language),
            Ban = Types.Ban.Map(account.Ban),
            Roles =
            {
                account.Roles
            }
        };

    public partial class Types
    {
        public partial class Ban
        {
            public static Ban? Map(Domain.ValueObjects.Ban? ban) =>
                ban switch
                {
                    not null => new Ban
                    {
                        Reason = ban.Reason,
                        ExpiresAt = ban.ExpiredAt.ToTimestamp()
                    },
                    _ => null
                };
        }
    }
}
