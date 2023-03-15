using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Queries.GetAccountDetails;

internal sealed record GetAccountDetailsQuery(long Id) : IRequest<Account>;

internal sealed class GetAccountDetailsQueryHandler : IRequestHandler<GetAccountDetailsQuery, Account>
{
    private readonly IAccountRepository _repository;

    public GetAccountDetailsQueryHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task<Account> Handle(GetAccountDetailsQuery request, CancellationToken ct) =>
        await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);
}
