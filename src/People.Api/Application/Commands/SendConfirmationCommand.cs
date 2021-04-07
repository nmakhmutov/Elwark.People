using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Api.Email.Models;
using People.Api.Infrastructure.EmailBuilder;
using People.Domain;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Infrastructure.Confirmations;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;

namespace People.Api.Application.Commands
{
    public sealed record SendConfirmationCommand(AccountId Id, EmailIdentity Email, Language Language)
        : IRequest<ObjectId>;

    internal sealed class SendConfirmationCommandHandler : IRequestHandler<SendConfirmationCommand, ObjectId>
    {
        private readonly IKafkaMessageBus _bus;
        private readonly IConfirmationService _confirmation;
        private readonly IEmailBuilder _emailBuilder;

        public SendConfirmationCommandHandler(IConfirmationService confirmation, IKafkaMessageBus bus,
            IEmailBuilder emailBuilder)
        {
            _confirmation = confirmation;
            _bus = bus;
            _emailBuilder = emailBuilder;
        }

        public async Task<ObjectId> Handle(SendConfirmationCommand request, CancellationToken ct)
        {
            var confirmation = await _confirmation.CreateAsync(request.Id, TimeSpan.FromMinutes(20), ct);
            var email = await _emailBuilder.CreateEmailAsync(
                $"Confirmation.{request.Language}.liquid",
                new ConfirmationCodeModel(confirmation.Code)
            );

            var evt = new EmailMessageCreatedIntegrationEvent(request.Email.Value, email.Subject, email.Body);
            await _bus.PublishAsync(evt, ct);

            return confirmation.Id;
        }
    }
}
