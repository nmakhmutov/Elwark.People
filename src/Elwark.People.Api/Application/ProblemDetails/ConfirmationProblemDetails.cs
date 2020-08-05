using System;

namespace Elwark.People.Api.Application.ProblemDetails
{
    public class ConfirmationProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public DateTimeOffset? RetryAfter { get; set; }
    }
}