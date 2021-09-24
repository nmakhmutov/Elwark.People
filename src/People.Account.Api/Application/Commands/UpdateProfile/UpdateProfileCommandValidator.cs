using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Account.Domain;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Infrastructure.Countries;
using People.Domain.Exceptions;

namespace People.Account.Api.Application.Commands.UpdateProfile
{
    public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator(ICountryService country)
        {
            bool BeAvailableTimeZone(string value)
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

            async Task<bool> BeAvailableCountry(string value, CancellationToken ct) =>
                await country.GetAsync(value, ct) is not null;
            
            RuleFor(x => x.Bio)
                .MaximumLength(260);

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .Must(x => x < DateTime.UtcNow);

            RuleFor(x => x.City)
                .MaximumLength(100);

            RuleFor(x => x.Gender)
                .IsInEnum();

            RuleFor(x => x.Language)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .Must(x => Language.TryParse(x, out _));

            RuleFor(x => x.Nickname)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .MinimumLength(3)
                .MaximumLength(Name.NicknameLength);

            RuleFor(x => x.FirstName)
                .MaximumLength(Name.FirstNameLength);

            RuleFor(x => x.LastName)
                .MaximumLength(Name.LastNameLength);

            RuleFor(x => x.TimeZone)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .Must(BeAvailableTimeZone).WithErrorCode(ElwarkExceptionCodes.TimeZoneNotFound);

            RuleFor(x => x.CountryCode)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .MustAsync(BeAvailableCountry).WithErrorCode(ElwarkExceptionCodes.CountryCodeNotFound);
        }
    }
}
