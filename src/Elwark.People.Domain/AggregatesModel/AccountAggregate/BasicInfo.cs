using System;
using System.Collections.Generic;
using System.Globalization;
using Elwark.People.Abstractions;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public class BasicInfo : ValueObject
    {
        public BasicInfo(CultureInfo language, Gender gender = Gender.Female, string timezone = "Atlantic/Azores",
            DateTime? birthday = null, string? bio = null)
        {
            Timezone = timezone;
            Gender = gender;
            Language = language;
            Bio = bio;
            Birthday = birthday;
        }

        public Gender Gender { get; }
        public CultureInfo Language { get; }
        public string Timezone { get; }
        public DateTime? Birthday { get; }
        public string? Bio { get; }

        public override string ToString() =>
            $"{Language.TwoLetterISOLanguageName} {Gender} {Timezone} {Birthday}".Trim();

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Bio;
            yield return Birthday;
            yield return Timezone;
            yield return Gender;
            yield return Language;
        }
    }
}