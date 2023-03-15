using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Google;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure;

namespace People.Api.Application.Commands.SignInByGoogle;

internal sealed record SignInByGoogleCommand(string Token, IPAddress Ip) : IRequest<SignInResult>;

internal sealed class SignInByGoogleCommandHandler : IRequestHandler<SignInByGoogleCommand, SignInResult>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IGoogleApiService _google;

    public SignInByGoogleCommandHandler(PeopleDbContext dbContext, IGoogleApiService google)
    {
        _dbContext = dbContext;
        _google = google;
    }

    public async Task<SignInResult> Handle(SignInByGoogleCommand request, CancellationToken ct)
    {
        var google = await _google
            .GetAsync(request.Token, ct)
            .ConfigureAwait(false);

        var result = await _dbContext.Accounts
            .AsNoTracking()
            .WhereGoogle(google.Identity)
            .Select(x => new SignInResult(x.Id, x.Name.FullName()))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? throw ExternalAccountException.NotFound(ExternalService.Google, google.Identity);

        return result;
    }
}
