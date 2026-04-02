using Mediator;
using People.Application.Providers.Postgres;
using People.Domain.Entities;

namespace People.Application.Queries.IsAccountActive;

public sealed record IsAccountActiveQuery(AccountId Id) : IQuery<bool>;

public sealed class IsAccountActiveQueryHandler : IQueryHandler<IsAccountActiveQuery, bool>
{
    private readonly INpgsqlAccessor _accessor;

    public IsAccountActiveQueryHandler(INpgsqlAccessor accessor) =>
        _accessor = accessor;

    public async ValueTask<bool> Handle(IsAccountActiveQuery request, CancellationToken ct)
    {
        var data = await _accessor.Sql("SELECT is_activated, ban IS NOT NULL FROM accounts WHERE id = @p0 LIMIT 1")
            .AddParameter("@p0", (long)request.Id)
            .Select(x => new
            {
                IsActivated = x.GetBoolean(0),
                IsBanned = x.GetBoolean(1)
            })
            .FirstOrDefaultAsync(ct);

        if (data is null)
            return false;

        await _accessor.Sql("UPDATE accounts SET updated_at = now() WHERE id = @p1;")
            .AddParameter("@p1", (long)request.Id)
            .ExecuteAsync(ct);

        return data is { IsActivated: true, IsBanned: false };
    }
}
