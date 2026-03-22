using FluentValidation;

namespace People.Api.Infrastructure.Filters;

internal sealed class ValidatorFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;
    private readonly IProblemDetailsFactory _problems;

    public ValidatorFilter(IValidator<T> validator, IProblemDetailsFactory problems)
    {
        _validator = validator;
        _problems = problems;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.SingleOrDefault(x => x?.GetType() == typeof(T)) is not T body)
            return Results.Problem(_problems.EmptyBody());

        var result = await _validator.ValidateAsync(body);

        if (result.IsValid)
            return await next(context);

        return Results.Problem(_problems.ToProblem(new ValidationException(result.Errors)));
    }
}
