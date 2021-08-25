using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Commands.AttachMicrosoft
{
    public sealed record AttachMicrosoftCommand(
        AccountId Id,
        Identity.Microsoft Microsoft,
        Identity.Email Email,
        string? FirstName,
        string? LastName
    ) : IRequest;

    internal sealed class AttachMicrosoftCommandHandler : IRequestHandler<AttachMicrosoftCommand>
    {
        private readonly IForbiddenService _forbidden;
        private readonly IAccountRepository _repository;

        public AttachMicrosoftCommandHandler(IAccountRepository repository, IForbiddenService forbidden)
        {
            _repository = repository;
            _forbidden = forbidden;
        }

        public async Task<Unit> Handle(AttachMicrosoftCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.AddMicrosoft(request.Microsoft, request.FirstName, request.LastName);
            if (await IsAvailableToAttach(request.Email, ct))
                account.AddEmail(request.Email, false);

            account.Update(account.Name with
                {
                    FirstName = account.Name.FirstName ?? request.FirstName,
                    LastName = account.Name.LastName ?? request.LastName
                },
                account.Address,
                account.TimeInfo,
                account.Language,
                account.Gender,
                account.Picture,
                account.Bio,
                account.DateOfBirth
            );

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }

        private async Task<bool> IsAvailableToAttach(Identity.Email email, CancellationToken ct)
        {
            if (await _repository.IsExists(email, ct))
                return false;

            var host = new MailAddress(email.Value).Host;
            if (await _forbidden.IsEmailHostDenied(host, ct))
                return false;

            return true;
        }
    }
}
