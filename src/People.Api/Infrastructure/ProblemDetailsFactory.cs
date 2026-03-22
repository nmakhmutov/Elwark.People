using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Infrastructure;

internal sealed class ProblemDetailsFactory : IProblemDetailsFactory
{
    public ProblemDetails EmptyBody() =>
        new()
        {
            Type = "Request:EmptyBody",
            Title = ProblemDetailsResources.GetString("EmptyBody_Title"),
            Detail = ProblemDetailsResources.GetString("EmptyBody_Detail"),
            Status = StatusCodes.Status400BadRequest
        };

    public ProblemDetails ToProblem(Exception exception) =>
        exception switch
        {
            ValidationException x => ValidationProblemDetailsBuilder.FromValidationException(x),
            ArgumentException x => ValidationProblemDetailsBuilder.FromArgumentException(x),
            PeopleException x => ProblemCatalog.FromPeopleException(x),
            ConfirmationException x => ProblemCatalog.FromConfirmationException(x),
            RpcException x => ToRpcProblem(x),
            _ => InternalProblem()
        };

    private static ProblemDetails ToRpcProblem(RpcException exception) =>
        ProblemCatalog.IsValidationRpcException(exception)
            ? ValidationProblemDetailsBuilder.FromRpcValidationException(exception)
            : ProblemCatalog.FromRpcException(exception);

    private static ProblemDetails InternalProblem() =>
        new()
        {
            Type = "Internal:Unhandled",
            Title = ProblemDetailsResources.GetString("Internal_ServerError_Title"),
            Detail = ProblemDetailsResources.GetString("Internal_ServerError_Detail"),
            Status = StatusCodes.Status500InternalServerError
        };
}
