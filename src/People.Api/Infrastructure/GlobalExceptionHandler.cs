using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;

namespace People.Api.Infrastructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var error = exception.ToProblem();

        var level = error.Status switch
        {
            401 => LogLevel.Warning,
            403 => LogLevel.Warning,
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
