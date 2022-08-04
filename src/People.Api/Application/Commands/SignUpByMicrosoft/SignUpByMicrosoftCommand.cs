using System.Net;
using MediatR;
using People.Api.Application.Models;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;

namespace People.Api.Application.Commands.SignUpByMicrosoft;

internal sealed record SignUpByMicrosoftCommand(string Token, Language Language, IPAddress Ip, string? UserAgent) :
    IRequest<SignUpResult>;

internal sealed class SignUpByMicrosoftCommandHandler : IRequestHandler<SignUpByMicrosoftCommand, SignUpResult>
{
    private readonly PeopleDbContext _dbContext;
    private readonly IIpHasher _hasher;
    private readonly IMicrosoftApiService _microsoft;
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SignUpByMicrosoftCommandHandler(PeopleDbContext dbContext, IMicrosoftApiService microsoft, IIpHasher hasher,
        IAccountRepository repository, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _microsoft = microsoft;
        _hasher = hasher;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<SignUpResult> Handle(SignUpByMicrosoftCommand request, CancellationToken ct)
    {
        var microsoft = await _microsoft.GetAsync(request.Token, ct)
            .ConfigureAwait(false);

        if (await _dbContext.Emails.IsEmailExistsAsync(microsoft.Email, ct).ConfigureAwait(false))
            throw EmailException.AlreadyCreated(microsoft.Email);

        if (await _dbContext.Connections.IsMicrosoftExistsAsync(microsoft.Identity, ct).ConfigureAwait(false))
            throw ExternalAccountException.AlreadyCreated(ExternalService.Microsoft, microsoft.Identity);

        var account = new Account(microsoft.Email.User, request.Language, request.Ip, _hasher);
        account.AddMicrosoft(microsoft.Identity, microsoft.FirstName, microsoft.LastName, _timeProvider);
        account.AddEmail(microsoft.Email, true, _timeProvider);

        await _repository.AddAsync(account, ct)
            .ConfigureAwait(false);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);

        return new SignUpResult(account.Id, account.Name.FullName());
    }
}
