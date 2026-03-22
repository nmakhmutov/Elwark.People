using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace People.Api.Infrastructure;

internal static class ValidationProblemDetailsBuilder
{
    public static ValidationProblemDetails FromValidationException(ValidationException exception) =>
        Build(GetValidationErrors(exception));

    public static ValidationProblemDetails FromArgumentException(ArgumentException exception) =>
        Build(new Dictionary<string, string[]>
        {
            [exception.ParamName ?? "@"] = [exception.Message]
        });

    public static ValidationProblemDetails FromRpcValidationException(RpcException exception) =>
        Build(GetValidationErrorsFromTrailers(exception.Trailers));

    private static Dictionary<string, string[]> GetValidationErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(t => t.ErrorMessage).ToArray());

    private static Dictionary<string, string[]> GetValidationErrorsFromTrailers(Metadata metadata) =>
        metadata.Where(x => x.Key.StartsWith("ex-field-", StringComparison.Ordinal))
            .GroupBy(x => x.Key)
            .ToDictionary(x => x.Key[9..], x => x.Select(t => t.Value).ToArray());

    private static ValidationProblemDetails Build(IDictionary<string, string[]> errors) =>
        new(errors)
        {
            Type = "ValidationException:InvalidModel",
            Title = ProblemDetailsResources.GetString("Validation_InvalidModel_Title"),
            Detail = BuildValidationDetail(errors),
            Status = StatusCodes.Status400BadRequest
        };

    private static string BuildValidationDetail(IDictionary<string, string[]> errors)
    {
        if (errors.Count == 0)
            return ProblemDetailsResources.GetString("Validation_InvalidModel_Detail");

        return string.Join('\n', errors.Select(kv =>
        {
            var msg = kv.Value.Length > 0 ? kv.Value[0] : string.Empty;
            return string.IsNullOrEmpty(msg) ? kv.Key : $"{kv.Key}: {msg}";
        }));
    }
}
