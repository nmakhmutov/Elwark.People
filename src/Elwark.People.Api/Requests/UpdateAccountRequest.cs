using System;
using System.Collections.Generic;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Requests
{
    public class UpdateAccountRequest
    {
        public UpdateAccountRequest(string language, Gender gender, DateTime? birthdate, string? firstName,
            string? lastName, string nickname, Uri picture, string timezone, string countryCode, string? city,
            string? bio, IDictionary<LinksType, Uri?> links)
        {
            Language = language;
            Gender = gender;
            Birthdate = birthdate;
            FirstName = firstName;
            LastName = lastName;
            Nickname = nickname;
            Picture = picture;
            Timezone = timezone;
            CountryCode = countryCode;
            City = city;
            Bio = bio;
            Links = links;
        }

        public string Language { get; }

        public Gender Gender { get; }

        public DateTime? Birthdate { get; }

        public string? FirstName { get; }

        public string? LastName { get; }

        public string Nickname { get; }

        public Uri Picture { get; }

        public string Timezone { get; }

        public string CountryCode { get; }

        public string? City { get; }

        public string? Bio { get; }

        public IDictionary<LinksType, Uri?> Links { get; }
    }
}