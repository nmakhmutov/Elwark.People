using Mediator;
using People.Application.Models;
using People.Application.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.SignInByMicrosoft;

public sealed record SignInByMicrosoftCommand(string Token) : IRequest<SignInResult>;

public sealed class SignInByMicrosoftCommandHandler : IRequestHandler<SignInByMicrosoftCommand, SignInResult>
{
    private readonly IAccountRepository _repository;
    private readonly IMicrosoftApiService _microsoft;
    private readonly TimeProvider _timeProvider;

    public SignInByMicrosoftCommandHandler(
        IAccountRepository repository,
        IMicrosoftApiService microsoft,
        TimeProvider timeProvider
    )
    {
        _repository = repository;
        _microsoft = microsoft;
        _timeProvider = timeProvider;
    }

    public async ValueTask<SignInResult> Handle(SignInByMicrosoftCommand request, CancellationToken ct)
    {
        var microsoft = await _microsoft.GetAsync(request.Token, ct);
        var account = await _repository.GetAsync(ExternalService.Microsoft, microsoft.Identity, ct)
            ?? throw ExternalAccountException.NotFound(ExternalService.Microsoft, microsoft.Identity);

        account.SignIn(_timeProvider);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return new SignInResult(account.Id, account.Name.FullName());
    }
}
