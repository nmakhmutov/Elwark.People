using Google.Protobuf.WellKnownTypes;
using People.Api.Application.Queries.GetAccountSummary;

// ReSharper disable CheckNamespace
namespace People.Grpc.People;

public partial class AccountReply
{
    internal static AccountReply Map(AccountSummary account) =>
        new()
        {
            Id = account.Id,
            Nickname = account.Name.Nickname,
            FirstName = account.Name.FirstName,
            LastName = account.Name.LastName,
            FullName = account.Name.FullName(),
            Picture = account.Picture,
            CountryCode = account.CountryCode.ToString(),
            TimeZone = account.TimeZone.ToString(),
            Language = account.Language.ToString(),
            Ban = Types.Ban.Map(account.Ban),
            Roles = { account.Roles }
        };

    public partial class Types
    {
        public partial class Ban
        {
            public static Ban? Map(Domain.ValueObjects.Ban? ban) =>
                ban switch
                {
                    not null => new Ban { Reason = ban.Reason, ExpiresAt = ban.ExpiredAt.ToTimestamp() },
                    _ => null
                };
        }
    }
}
