using FluentValidation;
using People.Domain.ValueObjects;

namespace People.Api.Application.Commands.UpdateAccount;

internal sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Language)
            .NotNull();

        RuleFor(x => x.Nickname)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(Name.NicknameLength);

        RuleFor(x => x.FirstName)
            .MaximumLength(Name.FirstNameLength);

        RuleFor(x => x.LastName)
            .MaximumLength(Name.LastNameLength);

        RuleFor(x => x.TimeZone)
            .NotNull();

        RuleFor(x => x.DateFormat)
            .NotNull();

        RuleFor(x => x.TimeFormat)
            .NotNull();

        RuleFor(x => x.StartOfWeek)
            .IsInEnum();

        RuleFor(x => x.Country)
            .NotNull();
    }
}
