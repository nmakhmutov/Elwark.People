using MediatR;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.ValueObjects;
using People.Infrastructure.Providers.NpgsqlData;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Api.Application.Queries.GetAccountSummary;

internal sealed record GetAccountSummaryQuery(AccountId Id) : IRequest<AccountSummary>;

internal sealed class GetAccountSummaryQueryHandler : IRequestHandler<GetAccountSummaryQuery, AccountSummary>
{
    private readonly INpgsqlDataProvider _dataProvider;

    public GetAccountSummaryQueryHandler(INpgsqlDataProvider dataProvider) =>
        _dataProvider = dataProvider;

    public async Task<AccountSummary> Handle(GetAccountSummaryQuery request, CancellationToken ct)
    {
        var sql = $"""
            SELECT a.id,
                   a.nickname,
                   a.first_name,
                   a.last_name,
                   a.prefer_nickname,
                   a.picture,
                   a.language,
                   a.continent_code,
                   a.country_code,
                   a.time_zone,
                   a.date_format,
                   a.time_format,
                   a.start_of_week,
                   a.roles,
                   a.ban
            FROM accounts a
            WHERE a.id = {request.Id}
            LIMIT 1
            """;

        return await _dataProvider
            .Sql(sql)
            .Select(x => new AccountSummary(
                new AccountId(x.GetInt64(0)),
                new Name(
                    x.GetString(1),
                    x.IsDBNull(2) ? null : x.GetString(2),
                    x.IsDBNull(3) ? null : x.GetString(3),
                    x.GetBoolean(4)
                ),
                x.GetString(5),
                Language.Parse(x.GetString(6)),
                ContinentCode.Parse(x.GetString(7)),
                CountryCode.Parse(x.GetString(8)),
                TimeZone.Parse(x.GetString(9)),
                DateFormat.Parse(x.GetString(10)),
                TimeFormat.Parse(x.GetString(11)),
                (DayOfWeek)x.GetInt32(12),
                x.GetFieldValue<string[]>(13),
                x.IsDBNull(14) ? null : x.GetFieldValue<Ban>(14)
            ))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);
    }
}

internal sealed record AccountSummary(
    AccountId Id,
    Name Name,
    string Picture,
    Language Language,
    ContinentCode ContinentCode,
    CountryCode CountryCode,
    TimeZone TimeZone,
    DateFormat DateFormat,
    TimeFormat TimeFormat,
    DayOfWeek StartOfWeek,
    string[] Roles,
    Ban? Ban
);
