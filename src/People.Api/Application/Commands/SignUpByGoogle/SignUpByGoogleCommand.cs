using System.Net;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure;

namespace People.Api.Application.Commands.SignUpByGoogle;

internal sealed record SignUpByGoogleCommand(string Token, Language Language, IPAddress Ip) : IRequest<SignUpResult>;

internal sealed class SignUpByGoogleCommandHandler : IRequestHandler<SignUpByGoogleCommand, SignUpResult>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IGoogleApiService _google;
    private readonly IIpHasher _hasher;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public SignUpByGoogleCommandHandler(PeopleDbContext dbContext, IGoogleApiService google, IIpHasher hasher,
        IAccountRepository repository, ITimeProvider time)
    {
        _dbContext = dbContext;
        _google = google;
        _hasher = hasher;
        _repository = repository;
        _time = time;
    }

    public async Task<SignUpResult> Handle(SignUpByGoogleCommand request, CancellationToken ct)
    {
        var google = await _google
            .GetAsync(request.Token, ct)
            .ConfigureAwait(false);

        if (await _dbContext.Emails.IsEmailExistsAsync(google.Email, ct).ConfigureAwait(false))
            throw EmailException.AlreadyCreated(google.Email);

        if (await _dbContext.Connections.IsGoogleExistsAsync(google.Identity, ct).ConfigureAwait(false))
            throw ExternalAccountException.AlreadyCreated(ExternalService.Google, google.Identity);

        var language = google.Locale is null
            ? request.Language
            : Language.Parse(google.Locale.TwoLetterISOLanguageName);

        var account = new Account(google.Email.User, language, google.Picture, request.Ip, _time, _hasher);
        account.AddGoogle(google.Identity, google.FirstName, google.LastName, _time);
        account.AddEmail(google.Email, google.IsEmailVerified, _time);

        await _repository
            .AddAsync(account, ct)
            .ConfigureAwait(false);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
