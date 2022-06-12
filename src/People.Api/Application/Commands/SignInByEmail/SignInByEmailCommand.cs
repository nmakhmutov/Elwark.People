using MediatR;
using Microsoft.EntityFrameworkCore;
using People.Api.Application.Models;
using People.Domain.Exceptions;
using People.Infrastructure;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SignInByEmail;

internal sealed record SignInByEmailCommand(string Token, int Code) : IRequest<SignInResult>;

internal sealed class SignInByEmailCommandHandler : IRequestHandler<SignInByEmailCommand, SignInResult>
{
    private readonly IConfirmationService _confirmation;
    private readonly PeopleDbContext _dbContext;

    public SignInByEmailCommandHandler(IConfirmationService confirmation, PeopleDbContext dbContext)
    {
        _confirmation = confirmation;
        _dbContext = dbContext;
    }

    public async Task<SignInResult> Handle(SignInByEmailCommand request, CancellationToken ct)
    {
        var confirmation = (await _confirmation.CheckSignInAsync(request.Token, request.Code))
            .GetOrThrow();

        var account =
            await _dbContext.Accounts
                .AsNoTracking()
                .Where(x => x.Id == confirmation.AccountId)
                .Select(x => new SignInResult(x.Id, x.Name.FullName()))
                .FirstOrDefaultAsync(ct) ?? throw AccountException.NotFound(confirmation.AccountId);

        return account;
    }
}
