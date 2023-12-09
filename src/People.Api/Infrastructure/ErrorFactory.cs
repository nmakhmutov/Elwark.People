using System.Net.Mime;
using System.Text;
using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using People.Domain.Exceptions;

namespace People.Api.Infrastructure;

internal static class ErrorFactory
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseExceptionHandler(builder => builder.Run(async context =>
        {
            var ex = context.Features.Get<IExceptionHandlerPathFeature>();
            if (ex is null)
                return;

            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            var error = ex.Error switch
            {
                ValidationException x => x.ToProblem(),
                RpcException x => x.ToProblem(),
                PeopleException x => x.ToProblem(),
                ArgumentException x => x.ToProblem(),
                _ => InternalProblem()
            };

            var level = error.Status > 499 ? LogLevel.Critical : LogLevel.Error;
            logger.Log(level, ex.Error, "Error in: {name}. {message}", ex.Path, ex.Error.Message);

            context.Response.ContentType = MediaTypeNames.Application.Json;
            context.Response.StatusCode = error.Status ?? StatusCodes.Status500InternalServerError;

            await context.Response
                .WriteAsJsonAsync(error);
        }));

    public static ValidationProblemDetails ToProblem(this ValidationException ex) =>
        ValidationProblemDetails(GetValidationErrors(ex));

    private static ValidationProblemDetails ToProblem(this ArgumentException ex) =>
        ValidationProblemDetails(new Dictionary<string, string[]> { [ex.ParamName ?? "@"] = new[] { ex.Message } });

    private static ProblemDetails ToProblem(this PeopleException exception)
    {
        // var title = Errors.ResourceManager.GetString($"{type}:Title");
        // var detail = Errors.ResourceManager.GetString($"{type}:Detail");

        var type = $"{exception.Name}:{exception.Code}";
        var title = $"{type}:Title";
        var detail = $"{type}:Detail";

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Detail = detail,
            Status = exception.Code == "NotFound"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest
        };

        switch (exception)
        {
            case AccountException x:
                problem.Extensions.Add("id", x.Id);
                break;

            case EmailException x:
                problem.Extensions.Add("email", x.Email.Address);
                break;

            case ExternalAccountException x:
                problem.Extensions.Add("service", x.Service.ToString());
                problem.Extensions.Add("identity", x.Identity);
                break;
        }

        return problem;
    }

    private static ProblemDetails ToProblem(this RpcException exception)
    {
        var name = exception.Trailers.FirstOrDefault(entry => entry.Key == "ex-name")?.Value;
        var code = exception.Trailers.FirstOrDefault(entry => entry.Key == "ex-code")?.Value;

        var type = $"{name}:{code}";
        var (title, detail) = Get(type);

        if (name == "ValidationException")
            return ValidationProblemDetails(GetValidationErrors(exception.Trailers));

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Detail = detail,
            Status = exception.StatusCode switch
            {
                StatusCode.NotFound => StatusCodes.Status404NotFound,
                StatusCode.InvalidArgument => StatusCodes.Status412PreconditionFailed,
                StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                StatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                StatusCode.FailedPrecondition => StatusCodes.Status412PreconditionFailed,
                StatusCode.OutOfRange => StatusCodes.Status412PreconditionFailed,
                StatusCode.Unimplemented => StatusCodes.Status501NotImplemented,
                StatusCode.Internal => StatusCodes.Status500InternalServerError,
                StatusCode.Unknown => StatusCodes.Status500InternalServerError,
                StatusCode.Unavailable => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            }
        };

        var id = exception.Trailers.FirstOrDefault(entry => entry.Key == "ex-id")?.Value;
        if (id is { Length: > 0 })
            problem.Extensions.Add("id", id);

        return problem;
    }

    private static ProblemDetails InternalProblem() =>
        new()
        {
            // Detail = Errors.Internal_Detail,
            // Title = Errors.Internal_Title,
            Detail = "Details",
            Title = "Title",
            Status = StatusCodes.Status500InternalServerError
        };

    private static Dictionary<string, string[]> GetValidationErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(t => GetValidationMessage(x.Key, t.ErrorCode)).ToArray()
            );

    private static Dictionary<string, string[]> GetValidationErrors(Metadata metadata) =>
        metadata.Where(x => x.Key.StartsWith("ex-field-"))
            .GroupBy(x => x.Key)
            .ToDictionary(
                x => x.Key[9..],
                x => x.SelectMany(t => t.Value.Split("|").Select(s => GetValidationMessage(x.Key[9..], s))).ToArray()
            );

    private static string GetValidationMessage(string propertyName, string code)
    {
        if (code is { Length: 0 })
            return "Unknown";

        // var template = Errors.ResourceManager.GetString($"ValidationException:Validator:{code}") ?? code;
        var template = $"ValidationException:Validator:{code}";
        return string.Format(template, propertyName);
    }

    private static ValidationProblemDetails ValidationProblemDetails(IDictionary<string, string[]> errors)
    {
        const string type = "ValidationException:InvalidModel";
        var (title, detail) = Get(type);

        var sb = new StringBuilder();
        foreach (var error in errors)
        {
            foreach (var s in error.Value)
                sb.Append(s).Append('.');

            sb.AppendLine();
        }

        return new ValidationProblemDetails(errors)
        {
            Title = title,
            Detail = sb.Length > 0 ? sb.ToString() : detail,
            Type = type,
            Status = StatusCodes.Status400BadRequest
        };
    }

    private static (string Title, string Detail) Get(string type) =>
    (
        // Errors.ResourceManager.GetString($"{type}:Title") ?? Errors.Internal_Title,
        // Errors.ResourceManager.GetString($"{type}:Detail") ?? Errors.Internal_Detail
        $"{type}:Title",
        $"{type}:Detail"
    );
}
