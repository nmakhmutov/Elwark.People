using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Infrastructure;
using People.Kafka.Integration;

namespace People.Api.Application.Commands.SignInByMicrosoft;

internal sealed record SignInByMicrosoftCommand(string Token, IPAddress Ip) : IRequest<SignInResult>;

internal sealed class SignInByMicrosoftCommandHandler : IRequestHandler<SignInByMicrosoftCommand, SignInResult>
{
    private readonly IIntegrationEventBus _bus;
    private readonly PeopleDbContext _dbContext;
    private readonly IMicrosoftApiService _microsoft;

    public SignInByMicrosoftCommandHandler(IIntegrationEventBus bus, PeopleDbContext dbContext,
        IMicrosoftApiService microsoft)
    {
        _bus = bus;
        _dbContext = dbContext;
        _microsoft = microsoft;
    }

    public async Task<SignInResult> Handle(SignInByMicrosoftCommand request, CancellationToken ct)
    {
        var ms = await _microsoft
            .GetAsync(request.Token, ct)
            .ConfigureAwait(false);

        var result = await _dbContext.Accounts
            .AsNoTracking()
            .WhereMicrosoft(ms.Identity)
            .Select(x => new SignInResult(x.Id, x.Name.FullName()))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? throw ExternalAccountException.NotFound(ExternalService.Microsoft, ms.Identity);

        var evt = new AccountEngaged.LoggedInIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, result.Id);
        await _bus.PublishAsync(evt, ct)
            .ConfigureAwait(false);

        return result;
    }
}
