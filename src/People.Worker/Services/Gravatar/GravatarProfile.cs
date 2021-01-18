using System;
using Newtonsoft.Json;

namespace People.Worker.Services.Gravatar
{
    public class GravatarProfile
    {
        public GravatarProfile(Uri profileUrl, string? preferredUsername, Uri thumbnailUrl, NameData? name,
            string? aboutMe)
        {
            ProfileUrl = profileUrl;
            PreferredUsername = preferredUsername;
            ThumbnailUrl = thumbnailUrl;
            Name = name;
            AboutMe = aboutMe;
        }

        [JsonProperty("profileUrl")]
        public Uri ProfileUrl { get; set; }

        [JsonProperty("preferredUsername")]
        public string? PreferredUsername { get; set; }

        [JsonProperty("thumbnailUrl")]
        public Uri ThumbnailUrl { get; set; }

        [JsonProperty("name")]
        public NameData? Name { get; set; }

        [JsonProperty("aboutMe")]
        public string? AboutMe { get; set; }

        public class NameData
        {
            [JsonProperty("givenName")]
            public string? FirstName { get; set; }

            [JsonProperty("familyName")]
            public string? LastName { get; set; }
        }
    }
}