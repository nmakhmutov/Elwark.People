using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Kafka;
using Integration.Event;
using MediatR;
using MongoDB.Bson;
using People.Api.Email.Models;
using People.Api.Infrastructure.EmailBuilder;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SendConfirmation;

public sealed record SendConfirmationCommand(AccountId Id, EmailIdentity Email, Language Language)
    : IRequest<ObjectId>;

internal sealed class SendConfirmationCommandHandler : IRequestHandler<SendConfirmationCommand, ObjectId>
{
    private readonly IEmailBuilder _builder;
    private readonly IKafkaMessageBus _bus;
    private readonly IConfirmationService _confirmation;

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
