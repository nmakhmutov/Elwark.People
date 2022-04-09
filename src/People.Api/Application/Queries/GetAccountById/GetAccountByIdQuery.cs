using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Queries.GetAccountById;

public sealed record GetAccountByIdQuery(AccountId Id) : IRequest<Account>;

internal sealed class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, Account>
{
    private readonly IAccountRepository _repository;

    public GetAccountByIdQueryHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task<Account> Handle(GetAccountByIdQuery request, CancellationToken ct) =>
        await _repository.GetAsync(request.Id, ct) ?? throw new PeopleException(ExceptionCodes.AccountNotFound);
}
