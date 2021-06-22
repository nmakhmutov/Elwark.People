using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;

namespace People.Api.Application.Queries
{
    public sealed record GetAccountByIdentityKeyQuery(Identity Key) : IRequest<Account?>;

    internal sealed class GetAccountByIdentityKeyQueryHandler : IRequestHandler<GetAccountByIdentityKeyQuery, Account?>
    {
        private readonly IAccountRepository _repository;

        public GetAccountByIdentityKeyQueryHandler(IAccountRepository repository) =>
            _repository = repository;

        public Task<Account?> Handle(GetAccountByIdentityKeyQuery request, CancellationToken ct) =>
            _repository.GetAsync(request.Key, ct);
    }
}
