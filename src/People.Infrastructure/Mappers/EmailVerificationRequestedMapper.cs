using People.Domain.DomainEvents;
using People.Domain.IntegrationEvents;
using People.Infrastructure.Outbox;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Mappers;

public sealed class EmailVerificationRequestedMapper : IOutboxEventMapper<EmailVerificationRequestedDomainEvent>
{
    public OutboxMessage Map(EmailVerificationRequestedDomainEvent evt)
    {
        var payload = new EmailVerificationRequestedIntegrationEvent(
            Guid.CreateVersion7(),
            evt.Id,
            evt.ConfirmationId,
            evt.Email.Address,
            evt.Language.ToString(),
            evt.OccurredAt
        );

        return OutboxMessage.Create(payload);
    }
}
