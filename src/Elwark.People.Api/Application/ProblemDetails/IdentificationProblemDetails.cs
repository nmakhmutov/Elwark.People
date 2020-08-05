using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.ProblemDetails
{
    public class IdentificationProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public Identification? Identifier { get; set; }
    }
}