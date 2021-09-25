using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Exceptions;

namespace People.Account.Api.Application.Queries.GetAccountById
{
    public sealed record GetAccountByIdQuery(AccountId Id) : IRequest<Domain.Aggregates.AccountAggregate.Account>;

    internal sealed class GetAccountByIdQueryHandler
        : IRequestHandler<GetAccountByIdQuery, Domain.Aggregates.AccountAggregate.Account>
    {
        private readonly IAccountRepository _repository;

        public GetAccountByIdQueryHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Domain.Aggregates.AccountAggregate.Account> Handle(GetAccountByIdQuery request, CancellationToken ct) =>
            await _repository.GetAsync(request.Id, ct) ??
            throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);
    }
}
