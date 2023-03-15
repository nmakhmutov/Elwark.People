using MediatR;
using People.Infrastructure.Providers.NpgsqlData;

namespace People.Api.Application.Queries.GetEmails;

internal sealed record GetEmailsQuery(long Id) : IRequest<IReadOnlyCollection<Email>>;

internal sealed class GetEmailsQueryHandler : IRequestHandler<GetEmailsQuery, IReadOnlyCollection<Email>>
{
    private readonly INpgsqlDataProvider _dataProvider;

    public GetEmailsQueryHandler(INpgsqlDataProvider dataProvider) =>
        _dataProvider = dataProvider;

    public async Task<IReadOnlyCollection<Email>> Handle(GetEmailsQuery request, CancellationToken ct) =>
        await _dataProvider
            .Sql($"SELECT email, is_primary, confirmed_at IS NOT NULL FROM emails WHERE account_id = {request.Id};")
            .Select(x => new Email(x.GetString(0), x.GetBoolean(1), x.GetBoolean(2)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

internal sealed record Email(string Value, bool IsPrimary, bool IsConfirmed);
