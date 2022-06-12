using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.ConfirmEmail;

internal sealed record ConfirmEmailCommand(string Token, int Code) : IRequest<EmailAccount>;

internal sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, EmailAccount>
{
    private readonly IConfirmationService _confirmation;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public ConfirmEmailCommandHandler(IConfirmationService confirmation, ITimeProvider time,
        IAccountRepository repository)
    {
        _confirmation = confirmation;
        _time = time;
        _repository = repository;
    }

    public async Task<EmailAccount> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var (id, email) = (await _confirmation.CheckEmailVerifyAsync(request.Token, request.Code)).GetOrThrow();
        
        var account = await _repository.GetAsync(id, ct) ?? throw AccountException.NotFound(id);
        account.ConfirmEmail(email, _time);

        _repository.Update(account);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return account.Emails.First(x => x.Email == email.Address);
    }
}
