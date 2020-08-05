using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Api.Infrastructure.Services.Google;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Application.Commands.AttachIdentity
{
    public class AttachGoogleIdentityCommand : IRequest
    {
        public AttachGoogleIdentityCommand(AccountId id, string accessToken)
        {
            Id = id;
            AccessToken = accessToken;
        }

        public AccountId Id { get; }

        public string AccessToken { get; }
    }

    public class AttachGoogleIdentityCommandHandler : IRequestHandler<AttachGoogleIdentityCommand>
    {
        private readonly IGoogleApiService _google;
        private readonly IOAuthIntegrationEventService _eventService;
        private readonly IAccountRepository _repository;
        private readonly IIdentificationValidator _validator;
        private readonly ILogger<AttachGoogleIdentityCommandHandler> _logger;

        public AttachGoogleIdentityCommandHandler(IAccountRepository repository, IGoogleApiService google,
            IOAuthIntegrationEventService eventService, IIdentificationValidator validator, ILogger<AttachGoogleIdentityCommandHandler> logger)
        {
            _repository = repository;
            _google = google;
            _eventService = eventService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(AttachGoogleIdentityCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct)
                          ?? throw ElwarkAccountException.NotFound(request.Id);

            var google = await _google.GetAsync(request.AccessToken, ct);

            await account.AddIdentificationAsync(google.Id, true, _validator);
            
            try
            {
                await account.AddIdentificationAsync(google.Email, google.IsEmailVerified, _validator);
            }
            catch(ElwarkException ex)
            {
                _logger.LogWarning(ex, "Error on added email");
            }

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(ct);

            await _eventService.PublishThroughEventBusAsync(new MergeAccountInformationIntegrationEvent
            {
                AccountId = account.Id,
                FirstName = google.FirstName,
                LastName = google.LastName,
                Picture = google.Picture
            }, ct);

            return Unit.Value;
        }
    }
}