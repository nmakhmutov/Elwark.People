using People.Gateway.Mappes;
using People.Grpc.Gateway;

namespace People.Gateway.Features.Account;

public sealed partial class AccountsController
{
    private static Models.Account ToAccount(ProfileReply account) =>
        new(
            account.Id.Value,
            account.Name.Nickname,
            account.Name.FirstName,
            account.Name.LastName,
            account.Name.FullName,
            account.Language,
            account.Picture,
            account.CountryCode,
            account.TimeZone,
            account.FirstDayOfWeek.FromGrpc(),
            account.Ban is not null
        );
}
