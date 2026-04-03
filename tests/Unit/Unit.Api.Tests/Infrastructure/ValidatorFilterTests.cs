using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Filters;
using Xunit;

namespace Unit.Api.Tests.Infrastructure;

/// <summary>Public so NSubstitute can build <see cref="IValidator{T}"/> proxies against strong-named FluentValidation.</summary>
public sealed class ValidatorFilterTestBody
{
    public string Name { get; set; } = "";
}

public sealed class ValidatorFilterTests
{
    [Fact]
    public async Task ValidRequest_InvokesNextDelegate()
    {
        var validator = Substitute.For<IValidator<ValidatorFilterTestBody>>();
        validator.ValidateAsync(Arg.Any<ValidatorFilterTestBody>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var problems = Substitute.For<IProblemDetailsFactory>();

        var filter = new ValidatorFilter<ValidatorFilterTestBody>(validator, problems);
        var body = new ValidatorFilterTestBody { Name = "ok" };
        var http = new DefaultHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(http, body);

        var nextInvoked = false;

        await filter.InvokeAsync(context, Next);

        Assert.True(nextInvoked);
        problems.DidNotReceive().ToProblem(Arg.Any<Exception>());
        return;

        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextInvoked = true;
            return new ValueTask<object?>(Results.Ok());
        }
    }

    [Fact]
    public async Task InvalidRequest_ReturnsValidationProblemUsingFactory()
    {
        var validator = Substitute.For<IValidator<ValidatorFilterTestBody>>();
        var failures = new[] { new ValidationFailure(nameof(ValidatorFilterTestBody.Name), "required") };
        validator.ValidateAsync(Arg.Any<ValidatorFilterTestBody>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var validationProblem = new ProblemDetails { Status = StatusCodes.Status400BadRequest };
        var problems = Substitute.For<IProblemDetailsFactory>();
        problems.ToProblem(Arg.Any<ValidationException>()).Returns(validationProblem);

        var filter = new ValidatorFilter<ValidatorFilterTestBody>(validator, problems);
        var body = new ValidatorFilterTestBody { Name = "" };
        var http = new DefaultHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(http, body);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(Results.Ok()));

        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Same(validationProblem, problemResult.ProblemDetails);

        problems.Received(1).ToProblem(Arg.Is<ValidationException>(ex =>
            ex.Errors.Single().PropertyName == nameof(ValidatorFilterTestBody.Name)));
    }

    [Fact]
    public async Task MissingBody_UsesEmptyBodyProblem()
    {
        var validator = Substitute.For<IValidator<ValidatorFilterTestBody>>();
        var problems = Substitute.For<IProblemDetailsFactory>();
        var empty = new ProblemDetails { Status = StatusCodes.Status400BadRequest };
        problems.EmptyBody().Returns(empty);

        var filter = new ValidatorFilter<ValidatorFilterTestBody>(validator, problems);
        var http = new DefaultHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(http, "not-a-body");

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(Results.Ok()));

        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Same(empty, problemResult.ProblemDetails);

        problems.Received(1).EmptyBody();
        await validator.DidNotReceive().ValidateAsync(Arg.Any<ValidatorFilterTestBody>(), Arg.Any<CancellationToken>());
    }
}
