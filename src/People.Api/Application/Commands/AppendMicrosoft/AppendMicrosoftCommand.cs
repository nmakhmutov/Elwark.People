using MediatR;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Infrastructure;

namespace People.Api.Application.Commands.AppendMicrosoft;

internal sealed record AppendMicrosoftCommand(AccountId Id, string Token) : IRequest;

internal sealed class AppendMicrosoftCommandHandler : IRequestHandler<AppendMicrosoftCommand>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IMicrosoftApiService _microsoft;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AppendMicrosoftCommandHandler(PeopleDbContext dbContext, IMicrosoftApiService microsoft,
        IAccountRepository repository, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _microsoft = microsoft;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task Handle(AppendMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        var microsoft = await _microsoft.GetAsync(request.Token, ct)
            .ConfigureAwait(false);

        if (!await _dbContext.Connections.IsMicrosoftExistsAsync(microsoft.Identity, ct).ConfigureAwait(false))
            account.AddMicrosoft(microsoft.Identity, microsoft.FirstName, microsoft.LastName, _timeProvider);

        if (!await _dbContext.Emails.IsEmailExistsAsync(microsoft.Email, ct).ConfigureAwait(false))
            account.AddEmail(microsoft.Email, true, _timeProvider);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);
    }
}
