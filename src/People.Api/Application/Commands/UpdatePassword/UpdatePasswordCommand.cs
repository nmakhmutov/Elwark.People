using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.UpdatePassword;

public sealed record UpdatePasswordCommand(AccountId Id, string OldPassword, string NewPassword) : IRequest;

internal sealed class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand>
{
    private readonly IPasswordHasher _hasher;
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public UpdatePasswordCommandHandler(IAccountRepository repository, IPasswordHasher hasher, IMediator mediator)
    {
        _repository = repository;
        _hasher = hasher;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdatePasswordCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        if (!account.IsPasswordEqual(request.OldPassword, _hasher))
            throw new PeopleException(ExceptionCodes.PasswordMismatch);

        account.SetPassword(request.NewPassword, _hasher, DateTime.UtcNow);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}