using System.Net;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;
using People.Infrastructure;

namespace People.Api.Application.Commands.SignUpByMicrosoft;

internal sealed record SignUpByMicrosoftCommand(string Token, Language Language, IPAddress Ip) : IRequest<SignUpResult>;

internal sealed class SignUpByMicrosoftCommandHandler : IRequestHandler<SignUpByMicrosoftCommand, SignUpResult>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IIpHasher _hasher;
    private readonly IMicrosoftApiService _microsoft;
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public SignUpByMicrosoftCommandHandler(PeopleDbContext dbContext, IMicrosoftApiService microsoft, IIpHasher hasher,
        IAccountRepository repository, ITimeProvider time)
    {
        _dbContext = dbContext;
        _microsoft = microsoft;
        _hasher = hasher;
        _repository = repository;
        _time = time;
    }

    public async Task<SignUpResult> Handle(SignUpByMicrosoftCommand request, CancellationToken ct)
    {
        var microsoft = await _microsoft.GetAsync(request.Token, ct);
        if (await _dbContext.Emails.IsEmailExistsAsync(microsoft.Email, ct))
            throw EmailException.AlreadyCreated(microsoft.Email);

        if (await _dbContext.Connections.IsMicrosoftExistsAsync(microsoft.Identity, ct))
            throw ExternalAccountException.AlreadyCreated(ExternalService.Microsoft, microsoft.Identity);

        var account = new Account(microsoft.Email.User, request.Language, null, request.Ip, _time, _hasher);
        account.AddMicrosoft(microsoft.Identity, microsoft.FirstName, microsoft.LastName, _time);
        account.AddEmail(microsoft.Email, true, _time);

        await _repository.AddAsync(account, ct);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
