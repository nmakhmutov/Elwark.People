using FluentValidation;
using People.Api.Endpoints;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Infrastructure.Validators;

public sealed class UpdateRequestValidatorTests
{
    private static readonly IValidator<AccountEndpoints.UpdateRequest> Validator =
        new AccountEndpoints.UpdateRequest.Validator();

    private static AccountEndpoints.UpdateRequest ValidRequest() =>
        new(
            FirstName: "Ann",
            LastName: "B",
            Nickname: "annb",
            UseNickname: false,
            Locale: "en",
            CountryCode: "DE",
            TimeZone: TimeZoneInfo.Utc.Id
        );

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = Validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyNickname_Fails(string nickname)
    {
        var result = Validator.Validate(ValidRequest() with { Nickname = nickname });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Nickname));
    }

    [Fact]
    public void NicknameShorterThanThreeChars_Fails()
    {
        var result = Validator.Validate(ValidRequest() with { Nickname = "ab" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Nickname));
    }

    [Fact]
    public void NicknameExceedingMaxLength_Fails()
    {
        var result = Validator.Validate(ValidRequest() with { Nickname = new string('x', Nickname.MaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Nickname));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyLocale_Fails(string locale)
    {
        var result = Validator.Validate(ValidRequest() with { Locale = locale });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Locale));
    }

    [Fact]
    public void LocaleLongerThanMaxLength_Fails()
    {
        var result = Validator.Validate(ValidRequest() with { Locale = new string('x', Locale.MaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Locale));
    }

    [Fact]
    public void InvalidTimeZone_FallsBackAndStillValid()
    {
        var result = Validator.Validate(ValidRequest() with { TimeZone = "Not/A/Real/Zone_Id" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InvalidCountryCode_ThrowsFormatException()
    {
        var request = ValidRequest() with { CountryCode = "D" };

        Assert.Throws<FormatException>(() => Validator.Validate(request));
    }
}
