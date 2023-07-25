using MediatR;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.ConfirmEmail;

internal sealed record ConfirmEmailCommand(string Token, string Code) : IRequest<EmailAccount>;

internal sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, EmailAccount>
{
    private readonly IConfirmationService _confirmation;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmEmailCommandHandler(IConfirmationService confirmation, TimeProvider timeProvider,
        IAccountRepository repository, ILogger<ConfirmEmailCommandHandler> logger)
    {
        _confirmation = confirmation;
        _timeProvider = timeProvider;
        _repository = repository;
        _logger = logger;
    }

    public async Task<EmailAccount> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var confirmation = await _confirmation.VerifyEmailAsync(request.Token, request.Code, ct)
            .ConfigureAwait(false);

        var account = await _repository.GetAsync(confirmation.AccountId, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(confirmation.AccountId);

        account.ConfirmEmail(confirmation.Email, _timeProvider);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        try
        {
            await _confirmation.DeleteAsync(account.Id, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while deleting confirmations for user {U}", account.Id);
        }

        return account.Emails
            .First(x => x.Email == confirmation.Email.Address);
    }
}
