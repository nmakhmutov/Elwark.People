using FluentValidation;
using People.Api.Application.Commands.UpdateAccount;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Api.Endpoints.Account.Requests;

internal sealed record UpdateRequest(string? FirstName, string? LastName, string Nickname, bool PreferNickname,
    string Language, string CountryCode, string TimeZone, string DateFormat, string TimeFormat, DayOfWeek StartOfWeek)
{
    public UpdateAccountCommand ToCommand(long id) =>
        new(
            id,
            FirstName,
            LastName,
            Nickname,
            PreferNickname,
            Domain.AggregatesModel.AccountAggregate.Language.Parse(Language),
            Domain.AggregatesModel.AccountAggregate.TimeZone.Parse(TimeZone),
            Domain.AggregatesModel.AccountAggregate.DateFormat.Parse(DateFormat),
            Domain.AggregatesModel.AccountAggregate.TimeFormat.Parse(TimeFormat),
            StartOfWeek,
            Domain.AggregatesModel.AccountAggregate.CountryCode.Parse(CountryCode)
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
                .Must(x => Domain.AggregatesModel.AccountAggregate.Language.TryParse(x, out _));

            RuleFor(x => x.CountryCode)
                .NotEmpty()
                .Length(2)
                .Must(x => Domain.AggregatesModel.AccountAggregate.CountryCode.TryParse(x, out _));

            RuleFor(x => x.TimeZone)
                .NotEmpty()
                .Must(x => Domain.AggregatesModel.AccountAggregate.TimeZone.TryParse(x, out _));

            RuleFor(x => x.DateFormat)
                .NotEmpty()
                .Must(x => Domain.AggregatesModel.AccountAggregate.DateFormat.TryParse(x, out _));

            RuleFor(x => x.TimeFormat)
                .NotEmpty()
                .Must(x => Domain.AggregatesModel.AccountAggregate.TimeFormat.TryParse(x, out _));

            RuleFor(x => x.StartOfWeek)
                .IsInEnum();
        }
    }
}
