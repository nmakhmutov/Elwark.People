using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Account.Api.Email.Models;
using People.Account.Api.Infrastructure.EmailBuilder;
using People.Account.Domain;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Infrastructure.Confirmations;
using People.Integration.Event;
using People.Kafka;

namespace People.Account.Api.Application.Commands.SendConfirmation
{
    public sealed record SendConfirmationCommand(AccountId Id, Identity.Email Email, Language Language)
        : IRequest<ObjectId>;

    internal sealed class SendConfirmationCommandHandler : IRequestHandler<SendConfirmationCommand, ObjectId>
    {
        private readonly IKafkaMessageBus _bus;
        private readonly IConfirmationService _confirmation;
        private readonly IEmailBuilder _builder;

        public SendConfirmationCommandHandler(IKafkaMessageBus bus, IConfirmationService confirmation,
            IEmailBuilder builder)
        {
            _bus = bus;
            _confirmation = confirmation;
            _builder = builder;
        }

        public async Task<ObjectId> Handle(SendConfirmationCommand request, CancellationToken ct)
        {
            var confirmation = await _confirmation.CreateAsync(request.Id, TimeSpan.FromMinutes(20), ct);
            var (subject, body) = await _builder.CreateEmailAsync(
                $"Confirmation.{request.Language}.liquid",
                new ConfirmationCodeModel(confirmation.Code)
            );

            await _bus.PublishAsync(
                EmailMessageCreatedIntegrationEvent.CreateNotDurable(request.Email.Value, subject, body),
                ct
            );

            return confirmation.Id;
        }
    }
}
