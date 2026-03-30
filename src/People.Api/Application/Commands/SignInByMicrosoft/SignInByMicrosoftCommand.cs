using System.Net;
using Mediator;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Kafka.Integration;

namespace People.Api.Application.Commands.SignInByMicrosoft;

internal sealed record SignInByMicrosoftCommand(string Token, IPAddress Ip, string? UserAgent) : IRequest<SignInResult>;

internal sealed class SignInByMicrosoftCommandHandler : IRequestHandler<SignInByMicrosoftCommand, SignInResult>
{
    private readonly IIntegrationEventBus _bus;
    private readonly IAccountRepository _repository;
    private readonly IMicrosoftApiService _microsoft;

    public SignInByMicrosoftCommandHandler(
        IIntegrationEventBus bus,
        IAccountRepository repository,
        IMicrosoftApiService microsoft
    )
    {
        _bus = bus;
        _repository = repository;
        _microsoft = microsoft;
    }

    public async ValueTask<SignInResult> Handle(SignInByMicrosoftCommand request, CancellationToken ct)
    {
        var microsoft = await _microsoft.GetAsync(request.Token, ct);

        var match = await _repository.GetAsync(
                ExternalService.Microsoft,
                microsoft.Identity,
                ct
            )
            ?? throw ExternalAccountException.NotFound(ExternalService.Microsoft, microsoft.Identity);

        var result = new SignInResult(match.Id, match.Name.FullName());

        var evt = new AccountActivity.LoggedInIntegrationEvent(result.Id);
        await _bus.PublishAsync(evt, ct);

        return result;
    }
}
