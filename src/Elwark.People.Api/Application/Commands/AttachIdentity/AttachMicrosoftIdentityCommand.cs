using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Api.Infrastructure.Services.Microsoft;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Application.Commands.AttachIdentity
{
    public class AttachMicrosoftIdentityCommand : IRequest
    {
        public AttachMicrosoftIdentityCommand(AccountId id, string accessToken)
        {
            Id = id;
            AccessToken = accessToken;
        }

        public AccountId Id { get; }

        public string AccessToken { get; }
    }

    public class AttachMicrosoftIdentityCommandHandler : IRequestHandler<AttachMicrosoftIdentityCommand>
    {
        private readonly IMicrosoftApiService _microsoftApi;
        private readonly IOAuthIntegrationEventService _eventService;
        private readonly IAccountRepository _repository;
        private readonly IIdentificationValidator _validator;
        private readonly ILogger<AttachMicrosoftIdentityCommandHandler> _logger;

        public AttachMicrosoftIdentityCommandHandler(IAccountRepository repository, IMicrosoftApiService microsoftApi, IOAuthIntegrationEventService eventService, IIdentificationValidator validator, ILogger<AttachMicrosoftIdentityCommandHandler> logger)
        {
            _repository = repository;
            _microsoftApi = microsoftApi;
            _eventService = eventService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(AttachMicrosoftIdentityCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct)
                          ?? throw ElwarkAccountException.NotFound(request.Id);

            var microsoft = await _microsoftApi.GetAsync(request.AccessToken, ct);

            await account.AddIdentificationAsync(microsoft.Id, true, _validator);
            try
            {
                await account.AddIdentificationAsync(microsoft.Email, false, _validator);
            }
            catch (ElwarkException ex)
            {
                _logger.LogWarning(ex, "Error on added email");
            }

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(ct);

            await _eventService.PublishThroughEventBusAsync(new MergeAccountInformationIntegrationEvent
            {
                AccountId = account.Id,
                FirstName = microsoft.FirstName,
                LastName = microsoft.LastName
            }, ct);

            return Unit.Value;
        }
    }
}