using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;
using People.Infrastructure.Countries;
using People.Infrastructure.Timezones;

namespace People.Api.Application.Commands.UpdateProfile
{
    public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator(ITimezoneService timezone, ICountryService country)
        {
            async Task<bool> BeAvailableTimezone(string value, CancellationToken ct) =>
                await timezone.GetAsync(value, ct) is not null;
            
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

            RuleFor(x => x.Timezone)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .MustAsync(BeAvailableTimezone).WithErrorCode(ElwarkExceptionCodes.TimezoneNotFound);

            RuleFor(x => x.CountryCode)
                .NotEmpty().WithErrorCode(ElwarkExceptionCodes.Required)
                .MustAsync(BeAvailableCountry).WithErrorCode(ElwarkExceptionCodes.CountryCodeNotFound);
        }
    }
}
