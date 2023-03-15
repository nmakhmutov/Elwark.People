using MediatR;
using People.Api.Application.Models;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SignUpByEmail;

internal sealed record SignUpByEmailCommand(string Token, string Code) : IRequest<SignUpResult>;

internal sealed class SignUpByEmailCommandHandler : IRequestHandler<SignUpByEmailCommand, SignUpResult>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public SignUpByEmailCommandHandler(IConfirmationService confirmation, IAccountRepository repository,
        ITimeProvider time)
    {
        _confirmation = confirmation;
        _repository = repository;
        _time = time;
    }

    public async Task<SignUpResult> Handle(SignUpByEmailCommand request, CancellationToken ct)
    {
        var confirmation = await _confirmation
            .SignUpAsync(request.Token, request.Code, ct)
            .ConfigureAwait(false);

        var account = await _repository
            .GetAsync(confirmation.AccountId, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(confirmation.AccountId);

        account.ConfirmEmail(account.GetPrimaryEmail(), _time);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
