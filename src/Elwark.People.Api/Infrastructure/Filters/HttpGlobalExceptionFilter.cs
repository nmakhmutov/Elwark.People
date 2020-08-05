using System;
using Elwark.People.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Infrastructure.Filters
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<HttpGlobalExceptionFilter> _logger;

        public HttpGlobalExceptionFilter(ILogger<HttpGlobalExceptionFilter> logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void OnException(ExceptionContext context)
        {
            var details = ExceptionHandler.CreateProblemDetails(context.Exception);

            context.HttpContext.Response.StatusCode = details.Status ?? StatusCodes.Status500InternalServerError;
            context.Result = ProblemDetailsExtensions.CreateProblemDetailsResponse(details);
            context.ExceptionHandled = true;

            _logger.Log(
                context.HttpContext.Response.StatusCode >= 500 ? LogLevel.Critical : LogLevel.Error,
                context.Exception,
                "Error in: {name}.{message}",
                context.ActionDescriptor.DisplayName,
                context.Exception.Message
            );
        }
    }
}