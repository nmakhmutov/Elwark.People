using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.AggregateModels.Account;

namespace People.Api.Application.Queries
{
    public sealed record GetAccountByIdQuery(AccountId Id) : IRequest<Account?>;
    
    internal sealed class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, Account?>
    {
        private readonly IAccountRepository _repository;

        public GetAccountByIdQueryHandler(IAccountRepository repository) =>
            _repository = repository;

        public Task<Account?> Handle(GetAccountByIdQuery request, CancellationToken ct) =>
            _repository.GetAsync(request.Id, ct);
    }
}