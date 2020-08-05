using System;
using FluentValidation;

namespace Elwark.People.Api.Extensions
{
    public static class FluentValidationExtensions
    {

        public static IRuleBuilderOptions<T, string?> Url<T>(this IRuleBuilder<T, string?> rule) =>
            rule.Must(s => Uri.TryCreate(s, UriKind.Absolute, out _))
                .WithMessage("'{PropertyName}' should be valid url format");
    }
}