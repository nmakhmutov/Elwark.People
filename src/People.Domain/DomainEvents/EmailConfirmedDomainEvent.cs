using System.Net.Mail;
using MediatR;
using People.Domain.Entities;

namespace People.Domain.DomainEvents;

public sealed record EmailConfirmedDomainEvent(Account Account, MailAddress Email) : INotification;
