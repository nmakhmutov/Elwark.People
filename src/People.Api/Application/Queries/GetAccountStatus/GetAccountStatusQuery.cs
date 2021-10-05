using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Api.Application.Queries.GetAccountStatus;

public sealed record GetAccountStatusQuery(AccountId Id) : IRequest<AccountStatus>;

public sealed class GetAccountStatusQueryHandler : IRequestHandler<GetAccountStatusQuery, AccountStatus>
{
    private readonly IAccountRepository _repository;

    public GetAccountStatusQueryHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task<AccountStatus> Handle(GetAccountStatusQuery request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);

        return new AccountStatus(account?.IsActive() ?? false);
    }
}
