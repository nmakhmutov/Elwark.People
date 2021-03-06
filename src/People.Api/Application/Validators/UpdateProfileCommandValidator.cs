using System;
using FluentValidation;
using People.Api.Application.Commands;
using People.Domain;
using People.Domain.Aggregates.Account;
using People.Infrastructure.Countries;
using People.Infrastructure.Timezones;

namespace People.Api.Application.Validators
{
    public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator(ITimezoneService timezone, ICountryService country)
        {
            RuleFor(x => x.Bio)
                .MaximumLength(260);

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .Must(x => x < DateTime.UtcNow);

            RuleFor(x => x.City)
                .MaximumLength(100);

            RuleFor(x => x.Gender)
                .IsInEnum();

            RuleFor(x => x.Language)
                .NotEmpty()
                .Must(x => Language.TryParse(x, out _));

            RuleFor(x => x.Nickname)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(Name.NicknameLength);

            RuleFor(x => x.FirstName)
                .MaximumLength(Name.FirstNameLength);

            RuleFor(x => x.LastName)
                .MaximumLength(Name.LastNameLength);

            RuleFor(x => x.Timezone)
                .NotEmpty()
                .MustAsync(async (value, ct) => await timezone.GetAsync(value, ct) is not null);

            RuleFor(x => x.CountryCode)
                .NotEmpty()
                .MustAsync(async (value, ct) => await country.GetAsync(value, ct) is not null);

            RuleFor(x => x.City)
                .NotNull();
        }
    }
}
