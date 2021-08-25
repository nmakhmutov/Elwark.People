using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Commands.AttachGoogle
{
    public sealed record AttachGoogleCommand(
        AccountId Id,
        Identity.Google Google,
        Identity.Email Email,
        string? FirstName,
        string? LastName,
        Uri? Picture,
        bool IsEmailVerified
    ) : IRequest;

    internal sealed class AttachGoogleCommandHandler : IRequestHandler<AttachGoogleCommand>
    {
        private readonly IForbiddenService _forbidden;
        private readonly IAccountRepository _repository;

        public AttachGoogleCommandHandler(IAccountRepository repository, IForbiddenService forbidden)
        {
            _repository = repository;
            _forbidden = forbidden;
        }

        public async Task<Unit> Handle(AttachGoogleCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            account.AddGoogle(request.Google, request.FirstName, request.LastName);
            if (await IsAvailableToAttach(request.Email, ct))
                account.AddEmail(request.Email, request.IsEmailVerified);

            account.Update(account.Name with
                {
                    FirstName = account.Name.FirstName ?? request.FirstName?[..Name.FirstNameLength],
                    LastName = account.Name.LastName ?? request.LastName?[..Name.LastNameLength]
                },
                request.Picture is not null && account.Picture == Account.DefaultPicture
                    ? request.Picture
                    : account.Picture
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
