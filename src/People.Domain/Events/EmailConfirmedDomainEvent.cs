using System.Net.Mail;
using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Domain.Events;

public sealed record EmailConfirmedDomainEvent(Account Account, MailAddress Email) : INotification;
