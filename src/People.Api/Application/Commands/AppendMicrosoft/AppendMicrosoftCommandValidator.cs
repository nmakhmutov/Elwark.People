using FluentValidation;

namespace People.Api.Application.Commands.AppendMicrosoft;

internal sealed class AppendMicrosoftCommandValidator : AbstractValidator<AppendMicrosoftCommand>
{
    public AppendMicrosoftCommandValidator() =>
        RuleFor(x => x.Token)
            .NotEmpty();
}
