using System.Collections.Frozen;
using System.Globalization;
using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using People.Domain.Exceptions;
using People.Infrastructure.Confirmations;

namespace People.Api.Infrastructure;

internal static class ProblemCatalog
{
    private static readonly FrozenDictionary<string, int> ConfirmationStatusByCode =
        new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["NotFound"] = StatusCodes.Status404NotFound,
                ["AlreadySent"] = StatusCodes.Status429TooManyRequests
            }
            .ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<StatusCode, int> GrpcStatusByCode =
        new Dictionary<StatusCode, int>
            {
                [StatusCode.NotFound] = StatusCodes.Status404NotFound,
                [StatusCode.InvalidArgument] = StatusCodes.Status412PreconditionFailed,
                [StatusCode.PermissionDenied] = StatusCodes.Status403Forbidden,
                [StatusCode.Unauthenticated] = StatusCodes.Status401Unauthorized,
                [StatusCode.FailedPrecondition] = StatusCodes.Status412PreconditionFailed,
                [StatusCode.OutOfRange] = StatusCodes.Status412PreconditionFailed,
                [StatusCode.Unimplemented] = StatusCodes.Status501NotImplemented,
                [StatusCode.Internal] = StatusCodes.Status500InternalServerError,
                [StatusCode.Unknown] = StatusCodes.Status500InternalServerError,
                [StatusCode.Unavailable] = StatusCodes.Status503ServiceUnavailable
            }
            .ToFrozenDictionary();

    public static bool IsValidationRpcException(RpcException exception) =>
        GetTrailer(exception.Trailers, "ex-name") == nameof(ValidationException);

    public static ProblemDetails FromConfirmationException(ConfirmationException exception)
    {
        var type = $"{nameof(ConfirmationException)}:{exception.Code}";
        var baseKey = $"{nameof(ConfirmationException)}_{exception.Code}";

        return new ProblemDetails
        {
            Type = type,
            Title = ProblemDetailsResources.GetString($"{baseKey}_Title"),
            Detail = ProblemDetailsResources.GetString($"{baseKey}_Detail"),
            Status = ConfirmationStatusByCode.GetValueOrDefault(exception.Code, StatusCodes.Status400BadRequest)
        };
    }

    public static ProblemDetails FromPeopleException(PeopleException exception)
    {
        var type = $"{exception.Name}:{exception.Code}";
        var baseKey = $"{exception.Name}_{exception.Code}";

        var problem = new ProblemDetails
        {
            Type = type,
            Title = ProblemDetailsResources.GetString($"{baseKey}_Title"),
            Detail = BuildPeopleDetail(exception, baseKey),
            Status = exception.Code == "NotFound" ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest
        };

        switch (exception)
        {
            case AccountException x:
                problem.Extensions["id"] = x.Id.ToString();
                break;

            case EmailException x:
                problem.Extensions["email"] = x.Email.Address;
                break;

            case ExternalAccountException x:
                problem.Extensions["service"] = x.Service.ToString();
                problem.Extensions["identity"] = x.Identity;
                break;
        }

        return problem;
    }

    public static ProblemDetails FromRpcException(RpcException exception)
    {
        var name = GetTrailer(exception.Trailers, "ex-name");
        var code = GetTrailer(exception.Trailers, "ex-code");

        var type = $"{name}:{code}";
        var baseKey = $"{name}_{code}";
        var titleKey = $"{baseKey}_Title";
        var detailKey = $"{baseKey}_Detail";
        var idTrailer = GetTrailer(exception.Trailers, "ex-id") ?? string.Empty;

        var title = ProblemDetailsResources.TryGetString(titleKey) ??
            throw new InvalidOperationException(
                $"Missing resource key '{titleKey}' for gRPC error (name={name}, code={code}). Add '{titleKey}' and '{detailKey}' to Resources/Errors.resx and Errors.ru.resx.");

        var detailTemplate = ProblemDetailsResources.TryGetString(detailKey);
        var detail = detailTemplate is not null
            ? FormatRpcDetail(name, detailTemplate, idTrailer)
            : string.IsNullOrWhiteSpace(exception.Status.Detail)
                ? ProblemDetailsResources.GetString("Rpc_Generic_Detail")
                : exception.Status.Detail;

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Detail = detail,
            Status = ResolveRpcStatus(name, code, exception.StatusCode)
        };

        if (idTrailer.Length > 0)
            problem.Extensions["id"] = idTrailer;

        return problem;
    }

    private static string? GetTrailer(Metadata metadata, string key) =>
        metadata.FirstOrDefault(entry => entry.Key == key)?.Value;

    private static int ResolveRpcStatus(string? name, string? code, StatusCode status) =>
        name switch
        {
            nameof(ConfirmationException) when code is not null =>
                ConfirmationStatusByCode.GetValueOrDefault(code, StatusCodes.Status400BadRequest),

            nameof(AccountException) or nameof(EmailException) or nameof(ExternalAccountException) =>
                code switch
                {
                    "NotFound" => StatusCodes.Status404NotFound,
                    "Forbidden" => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status400BadRequest
                },
            _ => GrpcStatusByCode.GetValueOrDefault(status, StatusCodes.Status400BadRequest)
        };

    private static string BuildPeopleDetail(PeopleException exception, string baseKey)
    {
        var key = $"{baseKey}_Detail";

        return exception switch
        {
            AccountException x => ProblemDetailsResources.GetString(key, x.Id.ToString()),
            EmailException x => ProblemDetailsResources.GetString(key, x.Email.Address),
            ExternalAccountException x => ProblemDetailsResources.GetString(key, x.Service.ToString(), x.Identity),
            _ => ProblemDetailsResources.GetString(key)
        };
    }

    private static string FormatRpcDetail(string? name, string template, string idTrailer)
    {
        var culture = CultureInfo.CurrentUICulture;

        return name switch
        {
            nameof(AccountException) or nameof(EmailException) => string.Format(culture, template, idTrailer),
            nameof(ExternalAccountException) => FormatExternalDetail(template, idTrailer),
            nameof(ConfirmationException) => template,
            _ => string.Format(culture, template, idTrailer)
        };
    }

    private static string FormatExternalDetail(string template, string trailer)
    {
        if (string.IsNullOrEmpty(trailer))
            return string.Empty;

        var idx = trailer.IndexOf(':');
        return idx < 0
            ? string.Format(CultureInfo.CurrentUICulture, template, trailer, string.Empty)
            : string.Format(CultureInfo.CurrentUICulture, template, trailer[..idx], trailer[(idx + 1)..]);
    }
}
