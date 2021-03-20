﻿using System.Net;
using MediatR;
using People.Domain.AggregateModels.Account;

namespace People.Domain.Events
{
    public sealed record AccountCreatedDomainEvent(Account Account, IPAddress IpAddress) : INotification;
}
