using FluentValidation;

namespace People.Api.Infrastructure.Filters;

internal sealed class ValidatorFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidatorFilter(IValidator<T> validator) =>
        _validator = validator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.SingleOrDefault(x => x?.GetType() == typeof(T)) is not T body)
            return Results.Problem(title: "Invalid model state", detail: "Body is empty", statusCode: 400);

        var result = await _validator.ValidateAsync(body);
        if (result.IsValid)
            return await next(context);

        return Results.Problem(new ValidationException(result.Errors).ToProblem());
    }
}
