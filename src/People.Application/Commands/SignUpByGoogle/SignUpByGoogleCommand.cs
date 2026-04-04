using System.Globalization;
using System.Net;
using Mediator;
using People.Application.Models;
using People.Application.Providers.Google;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;

namespace People.Application.Commands.SignUpByGoogle;

public sealed record SignUpByGoogleCommand(string Token, Language Language, CultureInfo Culture, IPAddress Ip)
    : ICommand<SignUpResult>;

public sealed class SignUpByGoogleCommandHandler : ICommandHandler<SignUpByGoogleCommand, SignUpResult>
{
    private readonly IGoogleApiService _google;
    private readonly IIpHasher _hasher;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SignUpByGoogleCommandHandler(
        IGoogleApiService google,
        IIpHasher hasher,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _google = google;
        _hasher = hasher;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<SignUpResult> Handle(SignUpByGoogleCommand request, CancellationToken ct)
    {
        var google = await _google.GetAsync(request.Token, ct);

        if (await _repository.IsExistsAsync(google.Email, ct))
            throw EmailException.AlreadyCreated(google.Email);

        if (await _repository.IsExistsAsync(ExternalService.Google, google.Identity, ct))
            throw ExternalAccountException.AlreadyCreated(ExternalService.Google, google.Identity);

        var culture = google.Locale ?? request.Culture;
        var account = Account.Create(request.Language, culture, request.Ip, _hasher, _timeProvider);
        account.AddGoogle(google.Identity, google.FirstName, google.LastName, google.Picture, _timeProvider);
        account.AddEmail(google.Email, google.IsEmailVerified, _timeProvider);

        await _repository.AddAsync(account, ct);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
