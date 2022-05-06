using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.CheckConfirmation;

internal sealed record CheckConfirmationCommand(string Token, uint Code) : IRequest;

internal sealed class CheckConfirmationCommandHandler : IRequestHandler<CheckConfirmationCommand>
{
    private readonly IConfirmationService _confirmation;

    public CheckConfirmationCommandHandler(IConfirmationService confirmation) =>
        _confirmation = confirmation;

    public async Task<Unit> Handle(CheckConfirmationCommand request, CancellationToken ct)
    {
        var confirmation = await _confirmation.GetAsync(request.Token, ct)
                   ?? throw new PeopleException(ExceptionCodes.ConfirmationNotFound);

        if (confirmation.Code != request.Code)
            throw new PeopleException(ExceptionCodes.ConfirmationNotMatch);

        return Unit.Value;
    }
}
