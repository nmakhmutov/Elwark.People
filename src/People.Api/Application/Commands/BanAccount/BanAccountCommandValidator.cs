using FluentValidation;

namespace People.Api.Application.Commands.BanAccount;

internal sealed class BanAccountCommandValidator : AbstractValidator<BanAccountCommand>
{
    public BanAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty();
    }
}
