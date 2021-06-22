using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Application.Commands.Attach
{
    public sealed record AttachGoogleCommand(
        AccountId Id,
        GoogleIdentity Google,
        EmailIdentity Email,
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

            account.AddGoogle(request.Google, GetName(request.FirstName, request.LastName));
            if (await IsAvailableToAttach(request.Email, ct))
                account.AddEmail(request.Email.GetMailAddress(), request.IsEmailVerified);

            account.Update(account.Name with
                {
                    FirstName = account.Name.FirstName ?? request.FirstName?[..Name.FirstNameLength],
                    LastName = account.Name.LastName ?? request.LastName?[..Name.LastNameLength]
                },
                account.Address,
                account.Timezone,
                account.Language,
                account.Gender,
                request.Picture is not null && account.Picture == Account.DefaultPicture
                    ? request.Picture
                    : account.Picture,
                account.Bio,
                account.DateOfBirth
            );

            await _repository.UpdateAsync(account, ct);

            return Unit.Value;
        }

        private async Task<bool> IsAvailableToAttach(EmailIdentity email, CancellationToken ct)
        {
            if (await _repository.IsExists(email, ct))
                return false;

            if (await _forbidden.IsEmailHostDenied(email.GetMailAddress().Host, ct))
                return false;

            return true;
        }

        private static string GetName(string? firstName, string? lastName)
        {
            var name = $"{firstName} {lastName}".Trim()[..Name.FullNameLength];

            return string.IsNullOrEmpty(name) ? "Unknown" : name;
        }
    }
}
