using Mediator;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token, string Code) : ICommand<EmailAccount>;

public sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, EmailAccount>
{
    private readonly IConfirmationChallengeService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly IEmailVerificationTokenService _tokens;

    public ConfirmEmailCommandHandler(
        IConfirmationChallengeService confirmation,
        IEmailVerificationTokenService tokens,
        TimeProvider timeProvider,
        IAccountRepository repository
    )
    {
        _confirmation = confirmation;
        _tokens = tokens;
        _timeProvider = timeProvider;
        _repository = repository;
    }

    public async ValueTask<EmailAccount> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var payload = _tokens.ParseToken(request.Token);
        var token = Convert.ToBase64String(payload.ConfirmationId.ToByteArray());
        var accountId = await _confirmation.VerifyAsync(token, request.Code, ConfirmationType.EmailConfirmation, ct);

        var account = await _repository.GetAsync(accountId, ct)
            ?? throw AccountException.NotFound(accountId);

        account.ConfirmEmail(payload.Email, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return account.Emails
            .First(x => x.Email == payload.Email.Address);
    }
}
