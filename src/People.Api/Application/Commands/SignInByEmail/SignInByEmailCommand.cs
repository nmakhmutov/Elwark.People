using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Models;
using People.Domain;
using People.Domain.Exceptions;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Kafka.Integration;

namespace People.Api.Application.Commands.SignInByEmail;

internal sealed record SignInByEmailCommand(string Token, string Code, IPAddress Ip, string? UserAgent) :
    IRequest<SignInResult>;

internal sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
{
    private readonly IIntegrationEventBus _bus;
    private readonly IConfirmationService _confirmation;
    private readonly PeopleDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SignInByEmailCommandHandler(IIntegrationEventBus bus, IConfirmationService confirmation,
        PeopleDbContext dbContext, TimeProvider timeProvider)
    {
        _bus = bus;
        _confirmation = confirmation;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
    {
        var id = await _confirmation.SignInAsync(request.Token, request.Code, ct)
            .ConfigureAwait(false);

        var result = await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SignInResult(x.Id, x.Name.FullName()))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(id);

        var evt = new AccountActivity.LoggedInIntegrationEvent(Guid.NewGuid(), _timeProvider.UtcNow(), result.Id);
        await _bus.PublishAsync(evt, ct)
            .ConfigureAwait(false);

        return result;
    }
}
