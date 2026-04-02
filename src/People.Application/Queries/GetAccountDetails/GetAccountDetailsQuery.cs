using Mediator;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Queries.GetAccountDetails;

public sealed record GetAccountDetailsQuery(AccountId Id) : IQuery<Account>;

public sealed class GetAccountDetailsQueryHandler : IQueryHandler<GetAccountDetailsQuery, Account>
{
    private readonly IAccountRepository _repository;

    public GetAccountDetailsQueryHandler(IAccountRepository repository) =>
        _repository = repository;

    public async ValueTask<Account> Handle(GetAccountDetailsQuery request, CancellationToken ct) =>
        await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);
}
