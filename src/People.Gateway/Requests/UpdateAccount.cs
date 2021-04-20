using System;
using FluentValidation;
using People.Grpc.Common;

namespace People.Gateway.Requests
{
    public sealed record UpdateAccount(string? FirstName, string? LastName, string Nickname, string Language,
        Gender Gender, DateTime Birthday, string? Bio, string? CountryCode, string? CityName, string Timezone)
    {
        public sealed class Validator : AbstractValidator<UpdateAccount>
        {
            public Validator()
            {
                RuleFor(x => x.Nickname)
                    .NotEmpty();

                RuleFor(x => x.Birthday)
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
            }
        }        
    }
}