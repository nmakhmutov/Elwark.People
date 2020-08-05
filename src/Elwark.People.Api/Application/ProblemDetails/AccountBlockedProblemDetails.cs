using System;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Application.ProblemDetails
{
    public class AccountBlockedProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public AccountId AccountId { get; set; }

        public BanType BanType { get; set; }
        
        public DateTimeOffset? ExpiredAt { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}