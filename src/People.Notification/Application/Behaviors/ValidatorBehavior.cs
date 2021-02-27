using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace People.Notification.Application.Behaviors
{
    public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> _logger;
        private readonly IValidator<TRequest>[] _validators;

        [DebuggerStepThrough]
        public ValidatorBehavior(IEnumerable<IValidator<TRequest>>? validators,
            ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
            _validators = validators?.ToArray() ?? Array.Empty<IValidator<TRequest>>();
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            var type = request.GetType().Name;

            var failures = _validators
                .Select(x => x.Validate(request))
                .SelectMany(x => x.Errors)
                .Where(x => x is not null)
                .ToArray();

            if (failures.Length == 0)
                return await next();

            _logger.LogWarning("Validation errors for '{T}' {R}. Errors: {E}", type, request, failures);

            throw new ValidationException($"Validation error in command {type}", failures);
        }
    }
}