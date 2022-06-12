using MediatR;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure;

namespace People.Api.Application.Commands.AppendGoogle;

internal sealed record AppendGoogleCommand(long Id, string Token) : IRequest;

internal sealed class AppendGoogleCommandHandler : IRequestHandler<AppendGoogleCommand>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IGoogleApiService _google;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public AppendGoogleCommandHandler(PeopleDbContext dbContext, IGoogleApiService google,
        IAccountRepository repository, ITimeProvider time)
    {
        _dbContext = dbContext;
        _google = google;
        _repository = repository;
        _time = time;
    }

    public async Task<Unit> Handle(AppendGoogleCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);
        var google = await _google.GetAsync(request.Token, ct);

        if (!await _dbContext.Connections.IsGoogleExistsAsync(google.Identity, ct))
            account.AddGoogle(google.Identity, google.FirstName, google.LastName, _time);

        if(!await _dbContext.Emails.IsEmailExistsAsync(google.Email, ct))
            account.AddEmail(google.Email, google.IsEmailVerified, _time);
        
        _repository.Update(account);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);
        
        return Unit.Value;
    }
}
