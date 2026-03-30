using System.Net.Mail;
using Mediator;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.AppendEmail;

internal sealed record AppendEmailCommand(AccountId Id, MailAddress Email) : IRequest<EmailAccount>;

internal sealed class AppendEmailCommandHandler : IRequestHandler<AppendEmailCommand, EmailAccount>
{
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AppendEmailCommandHandler(TimeProvider timeProvider, IAccountRepository repository)
    {
        _timeProvider = timeProvider;
        _repository = repository;
    }

    public async ValueTask<EmailAccount> Handle(AppendEmailCommand request, CancellationToken ct)
    {
        if (await _repository.IsExistsAsync(request.Email, ct))
            throw EmailException.AlreadyCreated(request.Email);

        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        account.AddEmail(request.Email, false, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return account.Emails
            .First(x => x.Email == request.Email.Address);
    }
}
