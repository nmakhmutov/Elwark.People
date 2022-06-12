using MediatR;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure;

namespace People.Api.Application.Commands.AppendMicrosoft;

internal sealed record AppendMicrosoftCommand(long Id, string Token) : IRequest;

internal sealed class AppendMicrosoftCommandHandler : IRequestHandler<AppendMicrosoftCommand>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IMicrosoftApiService _microsoft;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public AppendMicrosoftCommandHandler(PeopleDbContext dbContext, IMicrosoftApiService microsoft,
        IAccountRepository repository, ITimeProvider time)
    {
        _dbContext = dbContext;
        _microsoft = microsoft;
        _repository = repository;
        _time = time;
    }

    public async Task<Unit> Handle(AppendMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);
        var microsoft = await _microsoft.GetAsync(request.Token, ct);
        
        if (!await _dbContext.Connections.IsMicrosoftExistsAsync(microsoft.Identity, ct))
            account.AddMicrosoft(microsoft.Identity, microsoft.FirstName, microsoft.LastName, _time);

        if(!await _dbContext.Emails.IsEmailExistsAsync(microsoft.Email, ct))
            account.AddEmail(microsoft.Email, true, _time);
        
        _repository.Update(account);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);
        
        return Unit.Value;
    }
}
