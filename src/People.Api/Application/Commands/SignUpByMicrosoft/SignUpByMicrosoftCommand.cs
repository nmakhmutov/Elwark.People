using System.Net;
using Mediator;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;

namespace People.Api.Application.Commands.SignUpByMicrosoft;

internal sealed record SignUpByMicrosoftCommand(string Token, Language Language, IPAddress Ip, string? UserAgent) :
    IRequest<SignUpResult>;

internal sealed class SignUpByMicrosoftCommandHandler : IRequestHandler<SignUpByMicrosoftCommand, SignUpResult>
{
    private readonly IIpHasher _hasher;
    private readonly IMicrosoftApiService _microsoft;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SignUpByMicrosoftCommandHandler(
        IMicrosoftApiService microsoft,
        IIpHasher hasher,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _microsoft = microsoft;
        _hasher = hasher;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<SignUpResult> Handle(SignUpByMicrosoftCommand request, CancellationToken ct)
    {
        var microsoft = await _microsoft.GetAsync(request.Token, ct);

        if (await _repository.IsExistsAsync(microsoft.Email, ct))
            throw EmailException.AlreadyCreated(microsoft.Email);

        if (await _repository.IsExistsAsync(ExternalService.Microsoft, microsoft.Identity, ct))
            throw ExternalAccountException.AlreadyCreated(ExternalService.Microsoft, microsoft.Identity);

        var account = Account.Create(microsoft.Email.User, request.Language, request.Ip, _hasher);
        account.AddMicrosoft(microsoft.Identity, microsoft.FirstName, microsoft.LastName, _timeProvider);
        account.AddEmail(microsoft.Email, true, _timeProvider);

        await _repository.AddAsync(account, ct);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
