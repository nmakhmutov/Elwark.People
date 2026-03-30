using Mediator;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.AppendMicrosoft;

internal sealed record AppendMicrosoftCommand(AccountId Id, string Token) : IRequest;

internal sealed class AppendMicrosoftCommandHandler : IRequestHandler<AppendMicrosoftCommand>
{
    private readonly IMicrosoftApiService _microsoft;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AppendMicrosoftCommandHandler(
        IMicrosoftApiService microsoft,
        IAccountRepository repository,
        TimeProvider timeProvider
    )
    {
        _microsoft = microsoft;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Unit> Handle(AppendMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        var microsoft = await _microsoft.GetAsync(request.Token, ct);

        if (!await _repository.IsExistsAsync(ExternalService.Microsoft, microsoft.Identity, ct))
            account.AddMicrosoft(microsoft.Identity, microsoft.FirstName, microsoft.LastName, _timeProvider);

        if (!await _repository.IsExistsAsync(microsoft.Email, ct))
            account.AddEmail(microsoft.Email, true, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return Unit.Value;
    }
}
