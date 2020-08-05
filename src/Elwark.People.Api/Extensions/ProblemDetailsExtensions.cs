using Elwark.People.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Extensions
{
    public static class ProblemDetailsExtensions
    {
        public static IActionResult InvalidModelStateResponseFactory(ActionContext context)
        {
            var details = ExceptionHandler.CreateProblemDetails(context.ModelState);

            return CreateProblemDetailsResponse(details);
        }

        public static IActionResult CreateProblemDetailsResponse(ProblemDetails details) =>
            new ObjectResult(details)
            {
                StatusCode = details.Status,
                ContentTypes = {"application/problem+json", "application/problem+xml"}
            };
    }
}