using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Queries.GetAccountByIdentity;

internal sealed record GetAccountByIdentityQuery(Identity Identity) : IRequest<Account>;

internal sealed record GetAccountByIdentityQueryHandler : IRequestHandler<GetAccountByIdentityQuery, Account>
{
    private readonly IAccountRepository _repository;

    public GetAccountByIdentityQueryHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task<Account> Handle(GetAccountByIdentityQuery request, CancellationToken ct) =>
        await _repository.GetAsync(request.Identity, ct) ??
        throw new PeopleException(ExceptionCodes.AccountNotFound);
}
