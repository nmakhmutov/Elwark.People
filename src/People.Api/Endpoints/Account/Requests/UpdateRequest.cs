using FluentValidation;
using People.Api.Application.Commands.UpdateAccount;
using People.Domain.Entities;
using People.Domain.ValueObjects;

namespace People.Api.Endpoints.Account.Requests;

internal sealed record UpdateRequest(
    string? FirstName,
    string? LastName,
    string Nickname,
    bool PreferNickname,
    string Language,
    string CountryCode,
    string TimeZone,
    string DateFormat,
    string TimeFormat,
    DayOfWeek StartOfWeek)
{
    public UpdateAccountCommand ToCommand(AccountId id) =>
        new(
            id,
            FirstName,
            LastName,
            Nickname,
            PreferNickname,
            Domain.ValueObjects.Language.Parse(Language),
            Domain.ValueObjects.TimeZone.Parse(TimeZone),
            Domain.ValueObjects.DateFormat.Parse(DateFormat),
            Domain.ValueObjects.TimeFormat.Parse(TimeFormat),
            StartOfWeek,
            Domain.ValueObjects.CountryCode.Parse(CountryCode)
        );

    internal sealed class Validator : AbstractValidator<UpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Nickname)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(Name.NicknameLength);

            RuleFor(x => x.FirstName)
                .MaximumLength(Name.FirstNameLength);

            RuleFor(x => x.LastName)
                .MaximumLength(Name.LastNameLength);

            RuleFor(x => x.Language)
                .NotEmpty()
                .Length(2)
                .Must(x => Domain.ValueObjects.Language.TryParse(x, out _));

            RuleFor(x => x.CountryCode)
                .NotEmpty()
                .Length(2)
                .Must(x => Domain.ValueObjects.CountryCode.TryParse(x, out _));

            RuleFor(x => x.TimeZone)
                .NotEmpty()
                .Must(x => Domain.ValueObjects.TimeZone.TryParse(x, out _));

            RuleFor(x => x.DateFormat)
                .NotEmpty()
                .Must(x => Domain.ValueObjects.DateFormat.TryParse(x, out _));

            RuleFor(x => x.TimeFormat)
                .NotEmpty()
                .Must(x => Domain.ValueObjects.TimeFormat.TryParse(x, out _));

            RuleFor(x => x.StartOfWeek)
                .IsInEnum();
        }
    }
}
