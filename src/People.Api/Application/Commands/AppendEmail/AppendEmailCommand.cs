using System.Net.Mail;
using MediatR;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Infrastructure;

namespace People.Api.Application.Commands.AppendEmail;

internal sealed record AppendEmailCommand(long Id, MailAddress Email) : IRequest<EmailAccount>;

internal sealed class AppendEmailCommandHandler : IRequestHandler<AppendEmailCommand, EmailAccount>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AppendEmailCommandHandler(PeopleDbContext dbContext, TimeProvider timeProvider,
        IAccountRepository repository)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _repository = repository;
    }

    public async Task<EmailAccount> Handle(AppendEmailCommand request, CancellationToken ct)
    {
        if (await _dbContext.Emails.IsEmailExistsAsync(request.Email, ct).ConfigureAwait(false))
            throw EmailException.AlreadyCreated(request.Email);

        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.AddEmail(request.Email, false, _timeProvider);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return account.Emails
            .First(x => x.Email == request.Email.Address);
    }
}
