using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Application.Models;
using People.Domain.AggregateModels.Account;

namespace People.Api.Application.Queries
{
    public sealed record GetAccountStatusQuery(AccountId Id) : IRequest<AccountStatusResult>;

    public sealed class GetAccountStatusQueryHandler : IRequestHandler<GetAccountStatusQuery, AccountStatusResult>
    {
        private readonly IAccountRepository _repository;

        public GetAccountStatusQueryHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<AccountStatusResult> Handle(GetAccountStatusQuery request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);

            return new AccountStatusResult(account?.IsActive() ?? false);
        }
    }
}
