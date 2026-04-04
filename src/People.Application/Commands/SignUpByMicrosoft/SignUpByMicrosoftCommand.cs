using System.Globalization;
using System.Net;
using Mediator;
using People.Application.Models;
using People.Application.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;

namespace People.Application.Commands.SignUpByMicrosoft;

public sealed record SignUpByMicrosoftCommand(
    string Token,
    Language Language,
    Timezone Timezone,
    CultureInfo Culture,
    IPAddress Ip
) : IRequest<SignUpResult>;

public sealed class SignUpByMicrosoftCommandHandler : IRequestHandler<SignUpByMicrosoftCommand, SignUpResult>
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

        var account = Account.Create(
            request.Language,
            request.Timezone,
            request.Culture,
            request.Ip,
            _hasher,
            _timeProvider
        );
        account.AddMicrosoft(microsoft.Identity, microsoft.FirstName, microsoft.LastName, _timeProvider);
        account.AddEmail(microsoft.Email, true, _timeProvider);

        await _repository.AddAsync(account, ct);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
