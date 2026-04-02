using System.Diagnostics.CodeAnalysis;

namespace People.Domain;

public static class StringExtensions
{
    public static bool HasValue([NotNullWhen(true)] this string? value) =>
        !string.IsNullOrWhiteSpace(value);

    public static bool HasNoValue([NotNullWhen(false)] this string? value) =>
        string.IsNullOrWhiteSpace(value);

    extension(string? value)
    {
        public string? TrimToNull() =>
            value.HasValue() ? value.Trim() : null;

        public string? NormalizeUrl() =>
            value.HasValue() ? value.IsValidHttpUrl() ? value : null : null;

        public bool IsValidHttpUrl()
        {
            if (value.HasNoValue())
                return false;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed))
                return false;

            return parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps;
        }
    }
}
