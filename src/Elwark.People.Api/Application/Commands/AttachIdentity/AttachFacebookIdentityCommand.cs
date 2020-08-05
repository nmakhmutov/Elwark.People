using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Api.Infrastructure.Services.Facebook;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Application.Commands.AttachIdentity
{
    public class AttachFacebookIdentityCommand : IRequest
    {
        public AttachFacebookIdentityCommand(AccountId id, string accessToken)
        {
            Id = id;
            AccessToken = accessToken;
        }

        public AccountId Id { get; }

        public string AccessToken { get; }
    }

    public class AttachFacebookIdentityCommandHandler : IRequestHandler<AttachFacebookIdentityCommand>
    {
        private readonly IFacebookApiService _facebookApi;
        private readonly IAccountRepository _repository;
        private readonly IOAuthIntegrationEventService _eventService;
        private readonly IIdentificationValidator _validator;
        private readonly ILogger<AttachFacebookIdentityCommandHandler> _logger;

        public AttachFacebookIdentityCommandHandler(IAccountRepository repository, IFacebookApiService facebookApi,
            IOAuthIntegrationEventService eventService, IIdentificationValidator validator, ILogger<AttachFacebookIdentityCommandHandler> logger)
        {
            _repository = repository;
            _facebookApi = facebookApi;
            _eventService = eventService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(AttachFacebookIdentityCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct)
                          ?? throw ElwarkAccountException.NotFound(request.Id);

            var facebook = await _facebookApi.GetAsync(request.AccessToken, ct);

            await account.AddIdentificationAsync(facebook.Id, true, _validator);

            try
            {
                await account.AddIdentificationAsync(facebook.Email, false, _validator);
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
                FirstName = facebook.FirstName,
                LastName = facebook.LastName,
                Picture = facebook.Picture,
                Gender = facebook.Gender,
                Birthday = facebook.Birthday
            }, ct);

            return Unit.Value;
        }
    }
}