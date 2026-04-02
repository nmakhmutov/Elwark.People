using FluentValidation;

namespace People.Application.Commands.AppendMicrosoft;

public sealed class AppendMicrosoftCommandValidator : AbstractValidator<AppendMicrosoftCommand>
{
    public AppendMicrosoftCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
