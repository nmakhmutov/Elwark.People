using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.ChangePrimaryEmail;

internal sealed record ChangePrimaryEmailCommand(AccountId Id, EmailIdentity Email) : IRequest;

internal sealed class ChangePrimaryEmailCommandHandler : IRequestHandler<ChangePrimaryEmailCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public ChangePrimaryEmailCommandHandler(IAccountRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(ChangePrimaryEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct)
                      ?? throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.SetAsPrimaryEmail(request.Email);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
