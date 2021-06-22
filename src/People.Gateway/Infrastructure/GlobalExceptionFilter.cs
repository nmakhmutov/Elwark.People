using System;
using System.Net.Mime;
using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;

namespace People.Gateway.Infrastructure
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void OnException(ExceptionContext context)
        {
            var error = context.Exception switch
            {
                ValidationException x => ErrorFactory.Create(x),
                RpcException x => ErrorFactory.Create(x),
                ArgumentException x => ErrorFactory.Create(x.ParamName, x.Message),
                _ => ErrorFactory.Create(context.Exception, StatusCodes.Status500InternalServerError)
            };
            var result = new ObjectResult(error)
            {
                StatusCode = error.Status,
                ContentTypes = new MediaTypeCollection
                {
                    MediaTypeNames.Application.Json,
                    MediaTypeNames.Application.Xml
                },
                DeclaredType = error.GetType()
            };

            _logger.Log(
                error.Status > 499 ? LogLevel.Critical : LogLevel.Error,
                context.Exception,
                "Error in: {name}. {message}",
                context.ActionDescriptor.DisplayName,
                context.Exception.Message
            );

            context.HttpContext.Response.StatusCode = error.Status ?? 500;
            context.Result = result;
            context.ExceptionHandled = true;
        }
    }
}
