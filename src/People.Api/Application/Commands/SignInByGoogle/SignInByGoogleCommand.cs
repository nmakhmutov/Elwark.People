using System.Net;
using Mediator;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Kafka.Integration;

namespace People.Api.Application.Commands.SignInByGoogle;

internal sealed record SignInByGoogleCommand(string Token, IPAddress Ip, string? UserAgent) : IRequest<SignInResult>;

internal sealed class SignInByGoogleCommandHandler : IRequestHandler<SignInByGoogleCommand, SignInResult>
{
    private readonly IIntegrationEventBus _bus;
    private readonly IAccountRepository _repository;
    private readonly IGoogleApiService _google;

    public SignInByGoogleCommandHandler(
        IIntegrationEventBus bus,
        IAccountRepository repository,
        IGoogleApiService google
    )
    {
        _bus = bus;
        _repository = repository;
        _google = google;
    }

    public async ValueTask<SignInResult> Handle(SignInByGoogleCommand request, CancellationToken ct)
    {
        var google = await _google.GetAsync(request.Token, ct);

        var match = await _repository.GetAsync(ExternalService.Google, google.Identity, ct)
            ?? throw ExternalAccountException.NotFound(ExternalService.Google, google.Identity);

        var result = new SignInResult(match.Id, match.Name.FullName());

        var evt = new AccountActivity.LoggedInIntegrationEvent(result.Id);
        await _bus.PublishAsync(evt, ct);

        return result;
    }
}
