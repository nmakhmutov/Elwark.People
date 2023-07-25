using MediatR;
using People.Api.Application.Models;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SignUpByEmail;

internal sealed record SignUpByEmailCommand(string Token, string Code) : IRequest<SignUpResult>;

internal sealed class SignUpByEmailCommandHandler : IRequestHandler<SignUpByEmailCommand, SignUpResult>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SignUpByEmailCommandHandler(IConfirmationService confirmation, IAccountRepository repository,
        TimeProvider timeProvider)
    {
        _confirmation = confirmation;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<SignUpResult> Handle(SignUpByEmailCommand request, CancellationToken ct)
    {
        var id = await _confirmation.SignUpAsync(request.Token, request.Code, ct)
            .ConfigureAwait(false);

        var account = await _repository.GetAsync(id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(id);

        account.ConfirmEmail(account.GetPrimaryEmail(), _timeProvider);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
