using System.Diagnostics;
using FluentValidation;
using MediatR;

namespace People.Api.Application.Behaviour;

internal sealed class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IValidator<TRequest>[] _validators;

    [DebuggerStepThrough]
    public ValidatorBehavior(IEnumerable<IValidator<TRequest>>? validators) =>
        _validators = validators?.ToArray() ?? Array.Empty<IValidator<TRequest>>();

    [DebuggerStepThrough]
    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, RequestHandlerDelegate<TResponse> next)
    {
        if (_validators.Length == 0)
            return await next();

        var results = await Task.WhenAll(_validators.Select(x => x.ValidateAsync(request, ct)));
        var failures = results.SelectMany(x => x.Errors).Where(x => x is not null).ToArray();
        if (failures.Length > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
