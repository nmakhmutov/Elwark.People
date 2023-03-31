using MediatR;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.ConfirmEmail;

internal sealed record ConfirmEmailCommand(string Token, string Code) : IRequest<EmailAccount>;

internal sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, EmailAccount>
{
    private readonly IConfirmationService _confirmation;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public ConfirmEmailCommandHandler(IConfirmationService confirmation, ITimeProvider time,
        IAccountRepository repository, ILogger<ConfirmEmailCommandHandler> logger)
    {
        _confirmation = confirmation;
        _time = time;
        _repository = repository;
        _logger = logger;
    }

    public async Task<EmailAccount> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var (id, email) = await _confirmation
            .VerifyEmailAsync(request.Token, request.Code, ct)
            .ConfigureAwait(false);

        var account = await _repository
            .GetAsync(id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(id);

        account.ConfirmEmail(email, _time);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        try
        {
            await _confirmation
                .DeleteAsync(account.Id, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while deleting confirmations for user {U}", account.Id);
        }

        return account.Emails
            .First(x => x.Email == email.Address);
    }
}
