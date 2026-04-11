using FluentValidation;
using People.Domain.ValueObjects;

namespace People.Application.Commands.UpdateAccount;

public sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Locale)
            .NotNull();

        RuleFor(x => x.Nickname)
            .NotNull();

        RuleFor(x => x.FirstName)
            .MaximumLength(Name.FirstNameLength);

        RuleFor(x => x.LastName)
            .MaximumLength(Name.LastNameLength);

        RuleFor(x => x.Timezone)
            .NotNull();

        RuleFor(x => x.Country)
            .NotNull();
    }
}
