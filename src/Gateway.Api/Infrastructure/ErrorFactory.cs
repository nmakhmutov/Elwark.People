using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Gateway.Api.Infrastructure;

public static class ErrorFactory
{
    private const string IntervalError = "Internal";
    private const string ValidationError = "Validation";
    private const string RpcError = "Rpc";
    private const string NotFoundError = "NotFound";

    public static ProblemDetails Create(RpcException exception) =>
        exception.StatusCode switch
        {
            StatusCode.InvalidArgument => new ValidationProblemDetails(
                exception.Trailers
                    .Where(x => x.Key.StartsWith("field-"))
                    .ToDictionary(x => x.Key[6..], x => x.Value.Split("|").ToArray())
            )
            {
                Title = exception.Status.Detail,
                Type = ValidationError,
                Status = StatusCodes.Status400BadRequest
            },

            StatusCode.OutOfRange => Create("@", exception.Message),
            StatusCode.AlreadyExists => Create("@", exception.Message),

            StatusCode.FailedPrecondition => new ProblemDetails
            {
                Title = exception.Status.Detail,
                Type = RpcError,
                Status = StatusCodes.Status412PreconditionFailed
            },

            StatusCode.NotFound => new ProblemDetails
            {
                Title = "NotFound",
                Type = NotFoundError,
                Status = StatusCodes.Status404NotFound
            },

            StatusCode.Unavailable => new ProblemDetails
            {
                Title = "Unavailable",
                Type = IntervalError,
                Status = StatusCodes.Status503ServiceUnavailable
            },

            _ => new ProblemDetails
            {
                Title = "Unknown",
                Type = RpcError,
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception.Message
            }
        };

    public static ValidationProblemDetails Create(string? name, string message) =>
        new(new Dictionary<string, string[]> { [name ?? ""] = new[] { message } })
        {
            Title = "InvalidModelState",
            Type = ValidationError,
            Status = StatusCodes.Status400BadRequest
        };

    public static ValidationProblemDetails Create(ValidationException ex)
    {
        var result = ex.Errors.GroupBy(x => x.PropertyName)
            .Select(x => new KeyValuePair<string, IEnumerable<string>>(x.Key, x.Select(t => t.ErrorMessage)))
            .ToDictionary(x => x.Key, x => x.Value.ToArray());

        return new ValidationProblemDetails(result)
        {
            Title = "InvalidModelState",
            Type = ValidationError,
            Status = StatusCodes.Status400BadRequest
        };
    }

    public static ValidationProblemDetails Create(ModelStateDictionary state) =>
        new(state)
        {
            Title = "InvalidModelState",
            Type = ValidationError,
            Status = StatusCodes.Status400BadRequest
        };

    public static ProblemDetails Create(Exception ex, int status = StatusCodes.Status400BadRequest) =>
        new()
        {
            Title = ex.GetType().Name,
            Type = IntervalError,
            Detail = ex.Message,
            Status = status
        };
}
