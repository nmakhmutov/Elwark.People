using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.CreatePassword;

public sealed record CreatePasswordCommand(AccountId Id, string Password) : IRequest;

public sealed class CreatePasswordCommandHandler : IRequestHandler<CreatePasswordCommand>
{
    private readonly IPasswordHasher _hasher;
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public CreatePasswordCommandHandler(IAccountRepository repository, IPasswordHasher hasher, IMediator mediator)
    {
        _repository = repository;
        _hasher = hasher;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(CreatePasswordCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.SetPassword(request.Password, _hasher, DateTime.UtcNow);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
