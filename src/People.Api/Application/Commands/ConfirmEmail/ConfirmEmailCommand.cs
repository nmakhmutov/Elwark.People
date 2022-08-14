using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
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
        var (id, email) = await _confirmation.VerifyEmailAsync(request.Token, request.Code, ct);

        var account = await _repository.GetAsync(id, ct) ?? throw AccountException.NotFound(id);
        account.ConfirmEmail(email, _time);

        _repository.Update(account);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        try
        {
            await _confirmation.DeleteAsync(account.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while deleting confirmations for user {U}", account.Id);
        }

        return account.Emails.First(x => x.Email == email.Address);
    }
}
