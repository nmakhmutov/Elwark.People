using System.Net.Mail;
using People.Domain.Events;
using People.Domain.ValueObjects;

namespace People.Domain.DomainEvents;

public sealed record EmailVerificationRequestedDomainEvent(
    AccountId Id,
    Guid ConfirmationId,
    MailAddress Email,
    Language Language,
    DateTime OccurredAt
) : IDomainEvent;
