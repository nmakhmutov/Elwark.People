using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure.Countries;

namespace People.Api.Application.Commands.UpdateAccount;

internal sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    private readonly ICountryService _country;

    public UpdateAccountCommandValidator(ICountryService country)
    {
        _country = country;

        RuleFor(x => x.Language)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
            .Must(x => Language.TryParse(x, out _));

        RuleFor(x => x.Nickname)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
            .MinimumLength(3)
            .MaximumLength(Name.NicknameLength);

        RuleFor(x => x.FirstName)
            .MaximumLength(Name.FirstNameLength);

        RuleFor(x => x.LastName)
            .MaximumLength(Name.LastNameLength);

        RuleFor(x => x.TimeZone)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
            .Must(BeAvailableTimeZone).WithErrorCode(ExceptionCodes.TimeZoneNotFound);

        RuleFor(x => x.DateFormat)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
            .Must(x => DateFormat.List.Contains(x));

        RuleFor(x => x.TimeFormat)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
            .Must(x => TimeFormat.List.Contains(x));
        
        RuleFor(x => x.WeekStart)
            .IsInEnum();
        
        RuleFor(x => x.CountryCode)
            .NotEmpty().WithErrorCode(ExceptionCodes.Required)
            .MustAsync(BeAvailableCountry).WithErrorCode(ExceptionCodes.CountryCodeNotFound);
    }

    private static bool BeAvailableTimeZone(string value)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> BeAvailableCountry(string value, CancellationToken ct) =>
        await _country.GetAsync(value, ct) is not null;
}
