using MediatR;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Infrastructure;

namespace People.Api.Application.Commands.AppendGoogle;

internal sealed record AppendGoogleCommand(long Id, string Token) : IRequest;

internal sealed class AppendGoogleCommandHandler : IRequestHandler<AppendGoogleCommand>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IGoogleApiService _google;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AppendGoogleCommandHandler(PeopleDbContext dbContext, IGoogleApiService google,
        IAccountRepository repository, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _google = google;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task Handle(AppendGoogleCommand request, CancellationToken ct)
    {
        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        var google = await _google
            .GetAsync(request.Token, ct)
            .ConfigureAwait(false);

        if (!await _dbContext.Connections.IsGoogleExistsAsync(google.Identity, ct).ConfigureAwait(false))
            account.AddGoogle(google.Identity, google.FirstName, google.LastName, _timeProvider);

        if (!await _dbContext.Emails.IsEmailExistsAsync(google.Email, ct).ConfigureAwait(false))
            account.AddEmail(google.Email, google.IsEmailVerified, _timeProvider);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);
    }
}
