using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Queries.CheckSignUpConfirmation;

public sealed record CheckSignUpConfirmationQuery(ObjectId ConfirmationId) : IRequest<Confirmation>;

public sealed class CheckSignUpConfirmationQueryHandler : IRequestHandler<CheckSignUpConfirmationQuery, Confirmation>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;

    public CheckSignUpConfirmationQueryHandler(IAccountRepository repository, IConfirmationService confirmation)
    {
        _repository = repository;
        _confirmation = confirmation;
    }

    public async Task<Confirmation> Handle(CheckSignUpConfirmationQuery request, CancellationToken ct)
    {
        var confirmation = await _confirmation.GetAsync(request.ConfirmationId, ct);
        if (confirmation is null)
            throw new PeopleException(ExceptionCodes.ConfirmationNotFound);

        var account = await _repository.GetAsync(confirmation.AccountId, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        if (account.IsActivated)
            throw new PeopleException(ExceptionCodes.IdentityAlreadyConfirmed);

        return confirmation;
    }
}
