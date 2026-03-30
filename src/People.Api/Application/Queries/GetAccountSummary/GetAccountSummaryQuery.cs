using Mediator;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.ValueObjects;
using People.Infrastructure.Providers.NpgsqlData;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Api.Application.Queries.GetAccountSummary;

internal sealed record GetAccountSummaryQuery(AccountId Id) : IRequest<AccountSummary>;

internal sealed class GetAccountSummaryQueryHandler : IRequestHandler<GetAccountSummaryQuery, AccountSummary>
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
                       a.prefer_nickname,
                       a.picture,
                       a.language,
                       a.region_code,
                       a.country_code,
                       a.time_zone,
                       a.date_format,
                       a.time_format,
                       a.start_of_week,
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
                    x.GetString(2),
                    x.IsDbNull(3) ? null : x.GetString(3),
                    x.IsDbNull(4) ? null : x.GetString(4),
                    x.GetBoolean(5)
                ),
                x.GetString(6),
                Language.Parse(x.GetString(7)),
                RegionCode.Parse(x.GetString(8)),
                CountryCode.Parse(x.GetString(9)),
                TimeZone.Parse(x.GetString(10)),
                DateFormat.Parse(x.GetString(11)),
                TimeFormat.Parse(x.GetString(12)),
                (DayOfWeek)x.GetInt32(13),
                x.GetFieldValue<string[]>(14),
                x.IsDbNull(15) ? null : x.GetFieldValue<Ban>(15)
            ))
            .FirstOrDefaultAsync(ct) ?? throw AccountException.NotFound(request.Id);
}

internal sealed record AccountSummary(
    AccountId Id,
    string Email,
    Name Name,
    string Picture,
    Language Language,
    RegionCode RegionCode,
    CountryCode CountryCode,
    TimeZone TimeZone,
    DateFormat DateFormat,
    TimeFormat TimeFormat,
    DayOfWeek StartOfWeek,
    string[] Roles,
    Ban? Ban
);
