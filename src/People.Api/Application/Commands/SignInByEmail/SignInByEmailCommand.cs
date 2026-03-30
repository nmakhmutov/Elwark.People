using System.Net;
using Mediator;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Models;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Infrastructure.Confirmations;
using People.Kafka.Integration;

namespace People.Api.Application.Commands.SignInByEmail;

internal sealed record SignInByEmailCommand(string Token, string Code, IPAddress Ip, string? UserAgent)
    : IRequest<SignInResult>;

internal sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
{
    private readonly IIntegrationEventBus _bus;
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;

    public SignInByEmailCommandHandler(
        IIntegrationEventBus bus,
        IConfirmationService confirmation,
        IAccountRepository repository
    )
    {
        _bus = bus;
        _confirmation = confirmation;
        _repository = repository;
    }

    public async ValueTask<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
    {
        var id = await _confirmation.SignInAsync(request.Token, request.Code, ct);

        var match = await _repository.GetSignInMatchAsync(id, ct) ?? throw AccountException.NotFound(id);

        var result = new SignInResult(match.Id, match.Name.FullName());

        var evt = new AccountActivity.LoggedInIntegrationEvent(result.Id);
        await _bus.PublishAsync(evt, ct);

        return result;
    }
}
