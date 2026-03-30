using FluentValidation;
using People.Api.Endpoints;
using Xunit;

namespace People.UnitTests.Infrastructure.Validators;

public sealed class VerifyRequestValidatorTests
{
    private static readonly IValidator<AccountEndpoints.VerifyRequest> Validator =
        new AccountEndpoints.VerifyRequest.Validator();

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = Validator.Validate(new AccountEndpoints.VerifyRequest("token-value", "123456"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "123")]
    [InlineData("   ", "123")]
    public void EmptyToken_Fails(string token, string code)
    {
        var result = Validator.Validate(new AccountEndpoints.VerifyRequest(token, code));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.VerifyRequest.Token));
    }

    [Theory]
    [InlineData("tok", "")]
    [InlineData("tok", "   ")]
    public void EmptyCode_Fails(string token, string code)
    {
        var result = Validator.Validate(new AccountEndpoints.VerifyRequest(token, code));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.VerifyRequest.Code));
    }
}
