using System;
using Elwark.People.Api.Application.Commands;
using Elwark.Storage.Client;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
    {
        public UpdateAccountCommandValidator(IElwarkStorageClient client)
        {
            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Gender)
                .IsInEnum()
                .NotNull();

            RuleFor(x => x.Picture)
                .NotNull()
                .Must(x => x.IsAbsoluteUri);

            RuleFor(x => x.Nickname)
                .NotEmpty();

            RuleFor(x => x.CountryCode)
                .Length(2)
                .NotEmpty()
                .MustAsync(async (code, token) => await client.Country.GetByCodeAsync(code, token) is not null);

            RuleFor(x => x.Timezone)
                .NotEmpty()
                .MustAsync(async (zone, token) =>
                    await client.Timezone.GetByTimezoneNameAsync(zone, token) is not null);

            RuleFor(x => x.Language)
                .Length(2)
                .NotEmpty()
                .MustAsync(async (language, token) =>
                    await client.Language.GetByCodeAsync(language, token) is not null);

            RuleFor(x => x.Birthdate)
                .LessThan(DateTime.Today)
                .When(x => x.Birthdate is { });

            RuleForEach(x => x.Links)
                .NotNull()
                .ChildRules(x =>
                {
                    x.RuleFor(t => t.Key)
                        .NotNull()
                        .IsInEnum();

                    x.RuleFor(t => t.Value)
                        .Must(t => t!.IsAbsoluteUri).When(t => t.Value is { });
                });
        }
    }
}