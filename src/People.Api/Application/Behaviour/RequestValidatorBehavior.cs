using System.Diagnostics;
using FluentValidation;
using Mediator;

namespace People.Api.Application.Behaviour;

internal sealed class RequestValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private readonly IReadOnlyList<IValidator<TRequest>> _validators;

    [DebuggerStepThrough]
    public RequestValidatorBehavior(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators.ToArray();

    [DebuggerStepThrough]
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken ct
    )
    {
        if (_validators.Count == 0)
            return await next(request, ct);

        var results = await Task.WhenAll(_validators.Select(x => x.ValidateAsync(request, ct)));

        var failures = results.SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .ToArray();

        if (failures.Length > 0)
            throw new ValidationException(failures);

        return await next(request, ct);
    }
}
