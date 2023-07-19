using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Models;
using People.Domain.Exceptions;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Kafka.Integration;

namespace People.Api.Application.Commands.SignInByEmail;

internal sealed record SignInByEmailCommand(string Token, string Code) : IRequest<SignInResult>;

internal sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
{
    private readonly IIntegrationEventBus _bus;
    private readonly IConfirmationService _confirmation;
    private readonly PeopleDbContext _dbContext;

    public SignInByEmailCommandHandler(IIntegrationEventBus bus, IConfirmationService confirmation,
        PeopleDbContext dbContext)
    {
        _bus = bus;
        _confirmation = confirmation;
        _dbContext = dbContext;
    }

    public async Task<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
    {
        var confirmation = await _confirmation.SignInAsync(request.Token, request.Code, ct)
            .ConfigureAwait(false);

        var result = await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.Id == confirmation.AccountId)
            .Select(x => new SignInResult(x.Id, x.Name.FullName()))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(confirmation.AccountId);

        await _confirmation.DeleteAsync(result.Id, ct)
            .ConfigureAwait(false);

        var evt = new AccountEngaged.LoggedInIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, result.Id);
        await _bus.PublishAsync(evt, ct)
            .ConfigureAwait(false);

        return result;
    }
}
