using System.Net.Mail;
using Mediator;
using People.Application.Providers.Confirmation;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.ConfirmingEmail;

public sealed record ConfirmingEmailCommand(AccountId Id, MailAddress Email) : ICommand<ConfirmingTokenModel>;

public sealed class ConfirmingEmailCommandHandler : ICommandHandler<ConfirmingEmailCommand, ConfirmingTokenModel>
{
    private readonly IConfirmationChallengeService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly IEmailVerificationTokenService _tokens;

    public ConfirmingEmailCommandHandler(
        IConfirmationChallengeService confirmation,
        IEmailVerificationTokenService tokens,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _confirmation = confirmation;
        _tokens = tokens;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<ConfirmingTokenModel> Handle(ConfirmingEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        var emailAccount = account.Emails
            .FirstOrDefault(x => x.Email == request.Email.Address) ?? throw EmailException.NotFound(request.Email);

        if (emailAccount.IsConfirmed)
            throw EmailException.AlreadyConfirmed(request.Email);

        var challenge = await _confirmation.IssueAsync(account.Id, ConfirmationType.EmailConfirmation, ct);

        account.RequestEmailVerification(request.Email, _timeProvider);
        var token = _tokens.CreateToken(challenge.Id, request.Email);

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return new ConfirmingTokenModel(token);
    }
}
