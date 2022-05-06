using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Kafka;
using MediatR;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Email.Models;
using People.Api.Infrastructure.EmailBuilder;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands.SendConfirmation;

public sealed record SendConfirmationCommand(AccountId Id, EmailIdentity Email, Language Language)
    : IRequest<string>;

internal sealed class SendConfirmationCommandHandler : IRequestHandler<SendConfirmationCommand, string>
{
    private readonly IEmailBuilder _builder;
    private readonly IIntegrationEventBus _bus;
    private readonly IConfirmationService _confirmation;

    public SendConfirmationCommandHandler(IIntegrationEventBus bus, IConfirmationService confirmation, IEmailBuilder builder)
    {
        _bus = bus;
        _confirmation = confirmation;
        _builder = builder;
    }

    public async Task<string> Handle(SendConfirmationCommand request, CancellationToken ct)
    {
        var template = $"Confirmation.{request.Language}.liquid";
        var confirmation = await GetOrCreateConfirmationAsync(request.Id, ct);
        var (subject, body) = await _builder.CreateEmailAsync(template, new ConfirmationCodeModel(confirmation.Code));

        var evt = EmailMessageCreatedIntegrationEvent.CreateNotDurable(request.Email.Value, subject, body);
        await _bus.PublishAsync(evt, ct);

        return confirmation.Id.ToString();
    }

    private async Task<Confirmation> GetOrCreateConfirmationAsync(AccountId id, CancellationToken ct)
    {
        var confirmation = await _confirmation.GetAsync(id, ct);
        if (confirmation is null)
            return await _confirmation.CreateAsync(id, TimeSpan.FromMinutes(20), ct);

        var now = DateTime.UtcNow;
        if ((now - confirmation.CreatedAt).TotalMinutes < 1)
            throw new PeopleException(ExceptionCodes.ConfirmationAlreadySent);

        if ((confirmation.ExpireAt - now).TotalMinutes < 3)
            await _confirmation.DeleteAsync(id, ct);

        return await _confirmation.CreateAsync(id, TimeSpan.FromMinutes(20), ct);
    }
}
