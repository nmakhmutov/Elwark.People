using Mediator;
using People.Application.Models;
using People.Application.Providers.Confirmation;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.SignInByEmail;

public sealed record SignInByEmailCommand(string Token, string Code) : ICommand<SignInResult>;

public sealed class SignInByEmailCommandHandler : ICommandHandler<SignInByEmailCommand, SignInResult>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SignInByEmailCommandHandler(
        IConfirmationService confirmation,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _confirmation = confirmation;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
    {
        var id = await _confirmation.SignInAsync(request.Token, request.Code, ct);
        var account = await _repository.GetAsync(id, ct) ?? throw AccountException.NotFound(id);
        account.SignIn(_timeProvider);

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);
        await _confirmation.DeleteAsync(id, ct);

        return new SignInResult(account.Id, account.Name.FullName());
    }
}
