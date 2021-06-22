using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.Attach
{
    public sealed record AttachEmailCommand(AccountId Id, EmailIdentity Email) : IRequest;

    internal sealed class AttachEmailCommandHandler : IRequestHandler<AttachEmailCommand>
    {
        private readonly IAccountRepository _repository;

        public AttachEmailCommandHandler(IAccountRepository repository) =>
            _repository = repository;

        public async Task<Unit> Handle(AttachEmailCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.AddEmail(request.Email.GetMailAddress(), false);

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }
    }
}
