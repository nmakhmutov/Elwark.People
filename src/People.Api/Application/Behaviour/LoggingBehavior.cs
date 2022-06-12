using System.Diagnostics;
using MediatR;

namespace People.Api.Application.Behaviour;

internal sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    [DebuggerStepThrough]
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    [DebuggerStepThrough]
    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, RequestHandlerDelegate<TResponse> next)
    {
        var name = request.GetType().Name;

        _logger.LogInformation("Handling command {Name} {Request}", name, request);
        var response = await next();
        _logger.LogInformation("Command {Name} handled", name);

        return response;
    }
}
