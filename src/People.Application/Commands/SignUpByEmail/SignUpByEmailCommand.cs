using Mediator;
using People.Application.Models;
using People.Application.Providers.Confirmation;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.SignUpByEmail;

public sealed record SignUpByEmailCommand(string Token, string Code) : ICommand<SignUpResult>;

public sealed class SignUpByEmailCommandHandler : ICommandHandler<SignUpByEmailCommand, SignUpResult>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SignUpByEmailCommandHandler(
        IConfirmationService confirmation,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _confirmation = confirmation;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<SignUpResult> Handle(SignUpByEmailCommand request, CancellationToken ct)
    {
        var id = await _confirmation.SignUpAsync(request.Token, request.Code, ct);
        var account = await _repository.GetAsync(id, ct) ?? throw AccountException.NotFound(id);

        account.ConfirmEmail(account.GetPrimaryEmail(), _timeProvider);

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
