using Mediator;
using People.Application.Providers.Postgres;
using People.Domain.Entities;

namespace People.Application.Queries.GetEmails;

public sealed record GetEmailsQuery(AccountId Id) : IQuery<IReadOnlyCollection<UserEmail>>;

public sealed class GetEmailsQueryHandler : IQueryHandler<GetEmailsQuery, IReadOnlyCollection<UserEmail>>
{
    private readonly INpgsqlAccessor _accessor;

    public GetEmailsQueryHandler(INpgsqlAccessor accessor) =>
        _accessor = accessor;

    public async ValueTask<IReadOnlyCollection<UserEmail>> Handle(GetEmailsQuery request, CancellationToken ct) =>
        await _accessor.Sql("SELECT email, is_primary, confirmed_at IS NOT NULL FROM emails WHERE account_id = @p0")
            .AddParameter("@p0", (long)request.Id)
            .Select(x => new UserEmail(x.GetString(0), x.GetBoolean(1), x.GetBoolean(2)))
            .ToListAsync(ct);
}

public sealed record UserEmail(string Email, bool IsPrimary, bool IsConfirmed);
