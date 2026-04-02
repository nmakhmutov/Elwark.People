using Mediator;
using People.Application.Models;
using People.Application.Providers.Google;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.SignInByGoogle;

public sealed record SignInByGoogleCommand(string Token) : IRequest<SignInResult>;

public sealed class SignInByGoogleCommandHandler : IRequestHandler<SignInByGoogleCommand, SignInResult>
{
    private readonly IAccountRepository _repository;
    private readonly IGoogleApiService _google;
    private readonly TimeProvider _timeProvider;

    public SignInByGoogleCommandHandler(
        IAccountRepository repository,
        IGoogleApiService google,
        TimeProvider timeProvider
    )
    {
        _repository = repository;
        _google = google;
        _timeProvider = timeProvider;
    }

    public async ValueTask<SignInResult> Handle(SignInByGoogleCommand request, CancellationToken ct)
    {
        var google = await _google.GetAsync(request.Token, ct);
        var account = await _repository.GetAsync(ExternalService.Google, google.Identity, ct)
            ?? throw ExternalAccountException.NotFound(ExternalService.Google, google.Identity);

        account.SignIn(_timeProvider);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return new SignInResult(account.Id, account.Name.FullName());
    }
}
