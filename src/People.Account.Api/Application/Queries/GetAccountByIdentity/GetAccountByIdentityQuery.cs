using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Queries.GetAccountByIdentity
{
    internal sealed record GetAccountByIdentityQuery(Identity Identity) 
        : IRequest<Domain.Aggregates.AccountAggregate.Account>;

    internal sealed record GetAccountByIdentityQueryHandler
        : IRequestHandler<GetAccountByIdentityQuery, Domain.Aggregates.AccountAggregate.Account>
    {
        private readonly IAccountRepository _repository;

        public GetAccountByIdentityQueryHandler(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<Domain.Aggregates.AccountAggregate.Account> Handle(GetAccountByIdentityQuery request, CancellationToken ct) =>
            await _repository.GetAsync(request.Identity, ct) ??
            throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);
    }
}
