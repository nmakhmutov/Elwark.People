using System;
using System.Data;
using FluentValidation;
using People.Grpc.Common;
using DayOfWeek = System.DayOfWeek;

namespace People.Gateway.Requests
{
    public sealed record UpdateAccount(string? FirstName, string? LastName, string Nickname, bool PreferNickname,
        string Language, Gender Gender, DateTime DateOfBirth, string? Bio, string? CountryCode, string? CityName,
        string Timezone, DayOfWeek FirstDayOfWeek)
    {
        public sealed class Validator : AbstractValidator<UpdateAccount>
        {
            public Validator()
            {
                RuleFor(x => x.Nickname)
                    .NotEmpty();

                RuleFor(x => x.DateOfBirth)
                    .NotEmpty()
                    .Must(x => x < DateTime.UtcNow);

                RuleFor(x => x.Gender)
                    .IsInEnum();

                RuleFor(x => x.Language)
                    .NotEmpty()
                    .Length(2);

                RuleFor(x => x.CountryCode)
                    .NotEmpty()
                    .Length(2);

                RuleFor(x => x.Timezone)
                    .NotEmpty();

                RuleFor(x => x.FirstDayOfWeek)
                    .IsInEnum();
            }
        }
    }
}
