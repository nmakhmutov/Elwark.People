using System.Diagnostics;
using MediatR;

namespace People.Api.Application.Behaviour;

internal sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, TRequest, Exception?> Before =
        LoggerMessage.Define<string, TRequest>(LogLevel.Information, new EventId(1, "RequestLogging"),
            "Handling command {CommandName} ({@Request})"
        );

    private static readonly Action<ILogger, string, TResponse, Exception?> After =
        LoggerMessage.Define<string, TResponse>(LogLevel.Information, new EventId(2, "ResponseLogging"),
            "Command {CommandName} handled - response: {@Response}"
        );

    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    [DebuggerStepThrough]
    public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    [DebuggerStepThrough]
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = request.GetType()
            .Name;

        Before(_logger, name, request, null);

        var response = await next()
            .ConfigureAwait(false);

        After(_logger, name, response, null);

        return response;
    }
}
