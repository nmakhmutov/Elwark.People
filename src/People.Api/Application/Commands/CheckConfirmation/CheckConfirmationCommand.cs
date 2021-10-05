using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.CheckConfirmation;

internal sealed record CheckConfirmationCommand(ObjectId Id, uint Code) : IRequest;

internal sealed class CheckConfirmationCommandHandler : IRequestHandler<CheckConfirmationCommand>
{
    private readonly IConfirmationService _confirmation;

    public CheckConfirmationCommandHandler(IConfirmationService confirmation) =>
        _confirmation = confirmation;

    public async Task<Unit> Handle(CheckConfirmationCommand request, CancellationToken ct)
    {
        var data = await _confirmation.GetAsync(request.Id, ct);
        if (data is null)
            throw new ElwarkException(ElwarkExceptionCodes.ConfirmationNotFound);

        if (data.Code != request.Code)
            throw new ElwarkException(ElwarkExceptionCodes.ConfirmationNotMatch);

        return Unit.Value;
    }
}
