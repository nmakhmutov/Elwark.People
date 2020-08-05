using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class BanAccountCommand : IRequest
    {
        [DebuggerStepThrough]
        public BanAccountCommand(long accountId, BanType type, DateTimeOffset? expiredAt, string reason)
        {
            AccountId = accountId;
            ExpiredAt = expiredAt;
            Reason = reason;
            Type = type;
        }

        public long AccountId { get; }

        public BanType Type { get; }

        public DateTimeOffset? ExpiredAt { get; }

        public string Reason { get; }
    }

    public class BanAccountCommandHandler : IRequestHandler<BanAccountCommand>
    {
        private readonly IAccountRepository _repository;

        public BanAccountCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(BanAccountCommand request, CancellationToken cancellationToken)
        {
            var account = await _repository.GetAsync(request.AccountId, cancellationToken)
                          ?? throw ElwarkAccountException.NotFound(request.AccountId);

            account.SetBan(new Ban(request.Type, DateTimeOffset.UtcNow, request.ExpiredAt, request.Reason));

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}