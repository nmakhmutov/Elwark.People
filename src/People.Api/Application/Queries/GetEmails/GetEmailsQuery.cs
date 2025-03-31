using MediatR;
using People.Domain.Entities;
using People.Infrastructure.Providers.NpgsqlData;

namespace People.Api.Application.Queries.GetEmails;

internal sealed record GetEmailsQuery(AccountId Id) : IRequest<IReadOnlyCollection<UserEmail>>;

internal sealed class GetEmailsQueryHandler : IRequestHandler<GetEmailsQuery, IReadOnlyCollection<UserEmail>>
{
    private readonly INpgsqlAccessor _accessor;

    public GetEmailsQueryHandler(INpgsqlAccessor accessor) =>
        _accessor = accessor;

    public async Task<IReadOnlyCollection<UserEmail>> Handle(GetEmailsQuery request, CancellationToken ct) =>
        await _accessor.Sql("SELECT email, is_primary, confirmed_at IS NOT NULL FROM emails WHERE account_id = @p0")
            .AddParameter("@p0", (long)request.Id)
            .Select(x => new UserEmail(x.GetString(0), x.GetBoolean(1), x.GetBoolean(2)))
            .ToListAsync(ct);
}

internal sealed record UserEmail(string Email, bool IsPrimary, bool IsConfirmed);
