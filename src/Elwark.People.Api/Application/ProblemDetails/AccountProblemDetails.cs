using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.ProblemDetails
{
    public class AccountProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public AccountId AccountId { get; set; }
    }
}