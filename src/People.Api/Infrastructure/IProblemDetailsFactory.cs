using Microsoft.AspNetCore.Mvc;

namespace People.Api.Infrastructure;

public interface IProblemDetailsFactory
{
    ProblemDetails ToProblem(Exception exception);

    /// <summary>Localized 400 when the request body is missing (minimal API filters).</summary>
    ProblemDetails EmptyBody();
}
