using FluentValidation;
using People.Api.Endpoints;
using Xunit;

namespace People.UnitTests.Infrastructure.Validators;

public sealed class EmailRequestValidatorTests
{
    private static readonly IValidator<AccountEndpoints.EmailRequest> Validator =
        new AccountEndpoints.EmailRequest.Validator();

    [Fact]
    public void ValidEmail_Passes()
    {
        var result = Validator.Validate(new AccountEndpoints.EmailRequest("user.name+tag@example.com"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyEmail_Fails(string email)
    {
        var result = Validator.Validate(new AccountEndpoints.EmailRequest(email));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.EmailRequest.Email));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@missing")]
    [InlineData("missing@")]
    public void InvalidEmailFormat_Fails(string email)
    {
        var result = Validator.Validate(new AccountEndpoints.EmailRequest(email));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.EmailRequest.Email));
    }
}
