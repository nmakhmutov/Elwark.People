using System;
using FluentValidation;

namespace Gateway.Api.Requests;

public sealed record UpdateAccount(string? FirstName, string? LastName, string Nickname, bool PreferNickname,
    string Language, string? CountryCode, string TimeZone, DayOfWeek FirstDayOfWeek)
{
    public sealed class Validator : AbstractValidator<UpdateAccount>
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

            RuleFor(x => x.FirstDayOfWeek)
                .IsInEnum();
        }
    }
}
