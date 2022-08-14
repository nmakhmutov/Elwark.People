using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure;

namespace People.Api.Application.Commands.SignInByMicrosoft;

internal sealed record SignInByMicrosoftCommand(string Token, IPAddress Ip) : IRequest<SignInResult>;

internal sealed class SignInByMicrosoftCommandHandler : IRequestHandler<SignInByMicrosoftCommand, SignInResult>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IMicrosoftApiService _microsoft;

    public SignInByMicrosoftCommandHandler(PeopleDbContext dbContext, IMicrosoftApiService microsoft)
    {
        _dbContext = dbContext;
        _microsoft = microsoft;
    }

    public async Task<SignInResult> Handle(SignInByMicrosoftCommand request, CancellationToken ct)
    {
        var microsoft = await _microsoft.GetAsync(request.Token, ct);
        
        var result =
            await _dbContext.Accounts
                .AsNoTracking()
                .WhereMicrosoft(microsoft.Identity)
                .Select(x => new SignInResult(x.Id, x.Name.FullName()))
                .FirstOrDefaultAsync(ct)
            ?? throw ExternalAccountException.NotFound(ExternalService.Google, microsoft.Identity);

        return result;
    }
}
