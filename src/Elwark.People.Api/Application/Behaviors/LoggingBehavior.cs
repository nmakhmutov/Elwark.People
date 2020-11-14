using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        [DebuggerStepThrough]
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            var requestType = request.GetType().GetGenericTypeName();

            _logger.LogInformation("Handling command {CommandName}. {@Command}", requestType, request);

            var response = await next();

            _logger.LogInformation("Command {CommandName} handled. {@Response}", requestType, response);

            return response;
        }
    }
}