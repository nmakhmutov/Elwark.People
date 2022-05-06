using System.Text.Json.Serialization;

namespace People.Worker.Services.Gravatar;

public sealed class GravatarProfile
{
    [JsonPropertyName("preferredUsername")]
    public string? PreferredUsername { get; init; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; init; }

    [JsonPropertyName("name")]
    public NameData[]? Name { get; init; }

    [JsonPropertyName("aboutMe")]
    public string? AboutMe { get; init; }

    public sealed class NameData
    {
        [JsonPropertyName("givenName")]
        public string? FirstName { get; init; }

        [JsonPropertyName("familyName")]
        public string? LastName { get; init; }
    }
}
