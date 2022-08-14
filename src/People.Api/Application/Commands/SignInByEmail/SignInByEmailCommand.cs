using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.Models;
using People.Domain.Exceptions;
using People.Infrastructure;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SignInByEmail;

internal sealed record SignInByEmailCommand(string Token, string Code) : IRequest<SignInResult>;

internal sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
{
    private readonly IConfirmationService _confirmation;
    private readonly PeopleDbContext _dbContext;
    private readonly ILogger<SignInByEmailCommandHandler> _logger;

    public SignInByEmailCommandHandler(IConfirmationService confirmation, PeopleDbContext dbContext,
        ILogger<SignInByEmailCommandHandler> logger)
    {
        _confirmation = confirmation;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
    {
        var confirmation = await _confirmation.SignInAsync(request.Token, request.Code, ct);

        var account = await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.Id == confirmation.AccountId)
            .Select(x => new SignInResult(x.Id, x.Name.FullName()))
            .FirstOrDefaultAsync(ct) ?? throw AccountException.NotFound(confirmation.AccountId);

        try
        {
            await _confirmation.DeleteAsync(account.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while deleting confirmations for user {U}", account.Id);
        }

        return account;
    }
}
