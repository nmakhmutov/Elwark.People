using System;
using FluentValidation;

namespace People.Gateway.Requests;

public sealed record UpdateAccountRequest(string? FirstName, string? LastName, string Nickname, bool PreferNickname,
    string Language, string? CountryCode, string TimeZone, string DateFormat, string TimeFormat, DayOfWeek WeekStart)
{
    public sealed class Validator : AbstractValidator<UpdateAccountRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Nickname)
                .NotEmpty();

            RuleFor(x => x.Language)
                .NotEmpty()
                .Length(2);

            RuleFor(x => x.CountryCode)
                .NotEmpty()
                .Length(2);

            RuleFor(x => x.TimeZone)
                .NotEmpty();

            RuleFor(x => x.DateFormat)
                .NotEmpty();

            RuleFor(x => x.TimeFormat)
                .NotEmpty();
            
            RuleFor(x => x.WeekStart)
                .IsInEnum();
        }
    }
}
