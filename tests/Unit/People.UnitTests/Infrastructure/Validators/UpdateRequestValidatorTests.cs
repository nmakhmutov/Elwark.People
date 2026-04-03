using FluentValidation;
using People.Api.Endpoints;
using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Infrastructure.Validators;

public sealed class UpdateRequestValidatorTests
{
    private static readonly IValidator<AccountEndpoints.UpdateRequest> Validator =
        new AccountEndpoints.UpdateRequest.Validator();

    private static AccountEndpoints.UpdateRequest ValidRequest() =>
        new(
            FirstName: "Ann",
            LastName: "B",
            Nickname: "annb",
            PreferNickname: false,
            Language: "en",
            CountryCode: "DE",
            TimeZone: TimeZoneInfo.Utc.Id,
            DateFormat: "yyyy-MM-dd",
            TimeFormat: "HH:mm",
            StartOfWeek: DayOfWeek.Tuesday
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
        var r = ValidRequest() with { Nickname = nickname };

        var result = Validator.Validate(r);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Nickname));
    }

    [Fact]
    public void NicknameShorterThanThreeChars_Fails()
    {
        var r = ValidRequest() with { Nickname = "ab" };

        var result = Validator.Validate(r);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Nickname));
    }

    [Fact]
    public void NicknameExceedingMaxLength_Fails()
    {
        var r = ValidRequest() with { Nickname = new string('x', Nickname.MaxLength + 1) };

        var result = Validator.Validate(r);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.Nickname));
    }

    /// <remarks>
    /// Language rules chain <c>Length(2)</c> with <c>Must(Language.TryParse)</c>. FluentValidation continues rule
    /// execution, and the generated <c>TryParse</c> throws <see cref="FormatException"/> for non-empty invalid values.
    /// </remarks>
    [Theory]
    [InlineData("e")]
    [InlineData("eng")]
    public void InvalidLanguageNotTwoChars_ThrowsFormatException(string language)
    {
        var r = ValidRequest() with { Language = language };

        Assert.Throws<FormatException>(() => Validator.Validate(r));
    }

    [Fact]
    public void InvalidLanguageValue_ThrowsFormatException()
    {
        var r = ValidRequest() with { Language = "iv" };

        Assert.Throws<FormatException>(() => Validator.Validate(r));
    }

    [Fact]
    public void InvalidTimeZone_ThrowsTimeZoneNotFoundException()
    {
        var r = ValidRequest() with { TimeZone = "Not/A/Real/Zone_Id" };

        Assert.Throws<TimeZoneNotFoundException>(() => Validator.Validate(r));
    }

    [Fact]
    public void InvalidDateFormat_ThrowsFormatException()
    {
        var r = ValidRequest() with { DateFormat = "not-a-format" };

        Assert.Throws<FormatException>(() => Validator.Validate(r));
    }

    [Fact]
    public void InvalidTimeFormat_ThrowsFormatException()
    {
        var r = ValidRequest() with { TimeFormat = "25:99" };

        Assert.Throws<FormatException>(() => Validator.Validate(r));
    }

    [Fact]
    public void InvalidCountryCode_ThrowsFormatException()
    {
        var r = ValidRequest() with { CountryCode = "D" };

        Assert.Throws<FormatException>(() => Validator.Validate(r));
    }

    [Fact]
    public void InvalidStartOfWeek_Fails()
    {
        var r = ValidRequest() with { StartOfWeek = (DayOfWeek)42 };

        var result = Validator.Validate(r);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountEndpoints.UpdateRequest.StartOfWeek));
    }
}
