using System;
using System.Collections.Generic;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Application.Models
{
    public class AccountModel
    {
        public AccountModel(AccountId id, string? firstName, string? lastName, string nickname,
            Gender gender, DateTime? birthdate, string? countryCode, string language, string? city, string? timezone,
            string? bio, Uri picture, ICollection<string> roles, IDictionary<LinksType, Uri?> links,
            DateTimeOffset createdAt, DateTimeOffset updatedAt)
        {
            Id = id;
            Gender = gender;
            FirstName = firstName;
            LastName = lastName;
            Nickname = nickname;
            Picture = picture;
            Birthdate = birthdate;
            Timezone = timezone;
            CountryCode = countryCode;
            Language = language;
            City = city;
            Bio = bio;
            Roles = roles;
            Links = links;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public AccountId Id { get; }

        public Gender Gender { get; }

        public string? FirstName { get; }

        public string? LastName { get; }

        public string FullName => string.Join(" ", FirstName, LastName)
            .Trim()
            .NullIfEmpty() ?? Nickname;

        public string Nickname { get; }

        public DateTime? Birthdate { get; }

        public Uri Picture { get; }

        public string? Timezone { get; }

        public string? CountryCode { get; }

        public string Language { get; }

        public string? City { get; }

        public string? Bio { get; }

        public DateTimeOffset CreatedAt { get; }

        public DateTimeOffset UpdatedAt { get; }

        public ICollection<string> Roles { get; }

        public IDictionary<LinksType, Uri?> Links { get; }
    }
}