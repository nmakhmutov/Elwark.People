using Mediator;
using People.Application.Providers.Postgres;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.ValueObjects;

namespace People.Application.Queries.GetAccountSummary;

public sealed record GetAccountSummaryQuery(AccountId Id) : IQuery<AccountSummary>;

public sealed class GetAccountSummaryQueryHandler : IQueryHandler<GetAccountSummaryQuery, AccountSummary>
{
    private readonly INpgsqlAccessor _accessor;

    public GetAccountSummaryQueryHandler(INpgsqlAccessor accessor) =>
        _accessor = accessor;

    public async ValueTask<AccountSummary> Handle(GetAccountSummaryQuery request, CancellationToken ct) =>
        await _accessor.Sql(
                """
                SELECT a.id,
                       e.email,
                       a.nickname,
                       a.first_name,
                       a.last_name,
                       a.use_nickname,
                       a.picture,
                       a.locale,
                       a.region_code,
                       a.country_code,
                       a.time_zone,
                       a.roles,
                       a.ban
                FROM accounts a
                         INNER JOIN emails e ON a.id = e.account_id AND e.is_primary = TRUE
                WHERE a.id = @p0
                LIMIT 1
                """
            )
            .AddParameter("@p0", (long)request.Id)
            .Select(x => new AccountSummary(
                new AccountId(x.GetInt64(0)),
                x.GetString(1),
                Name.Create(
                    Nickname.Parse(x.GetString(2)),
                    x.IsDbNull(3) ? null : x.GetString(3),
                    x.IsDbNull(4) ? null : x.GetString(4),
                    x.GetBoolean(5)
                ),
                Picture.Parse(x.GetString(6)),
                Locale.Parse(x.GetString(7)),
                RegionCode.Parse(x.GetString(8)),
                CountryCode.Parse(x.GetString(9)),
                Timezone.Parse(x.GetString(10)),
                x.GetFieldValue<string[]>(11),
                x.IsDbNull(12) ? null : x.GetFieldValue<Ban>(12)
            ))
            .FirstOrDefaultAsync(ct) ?? throw AccountException.NotFound(request.Id);
}

public sealed record AccountSummary(
    AccountId Id,
    string Email,
    Name Name,
    Picture Picture,
    Locale Locale,
    RegionCode RegionCode,
    CountryCode CountryCode,
    Timezone Timezone,
    string[] Roles,
    Ban? Ban
);
