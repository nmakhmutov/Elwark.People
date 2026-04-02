using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace People.Application.Behaviour;

public sealed partial class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    [DebuggerStepThrough]
    public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    [DebuggerStepThrough]
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken ct
    )
    {
        var name = request.GetType().Name;

        Handling(_logger, name, request);

        var response = await next(request, ct);

        Handled(_logger, name, response);

        return response;
    }

    [LoggerMessage(LogLevel.Information, "Executing {Command} {@Request}")]
    private static partial void Handling(ILogger logger, string command, TRequest request);

    [LoggerMessage(LogLevel.Debug, "Executed {Command} {@Response}")]
    private static partial void Handled(ILogger logger, string command, TResponse response);
}
