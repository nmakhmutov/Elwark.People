using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;

namespace People.Api.Infrastructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsFactory _factory;

    public GlobalExceptionHandler(IProblemDetailsFactory factory, ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
        _factory = factory;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var error = _factory.ToProblem(exception);

        var level = error.Status switch
        {
            401 or 403 => LogLevel.Warning,
            404 => LogLevel.Debug,
            > 400 and < 500 => LogLevel.Error,
            _ => LogLevel.Critical
        };

        if (_logger.IsEnabled(level))
            _logger.Log(level, exception, "Error in: {Endpoint}", context.Request.GetDisplayUrl());

        if (Activity.Current is not null)
            context.Response.Headers.TryAdd("TraceId", Activity.Current.TraceId.ToString());

        await Results.Problem(error)
            .ExecuteAsync(context);

        return true;
    }
}
