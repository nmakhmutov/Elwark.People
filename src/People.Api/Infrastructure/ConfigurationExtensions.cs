namespace People.Api.Infrastructure;

internal static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public string GetRequiredString(string key)
        {
            var value = configuration[key];

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Configuration value '{key}' is required.");

            return value;
        }

        public Uri GetRequiredUri(string key)
        {
            var value = configuration.GetRequiredString(key);

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Configuration value '{key}' must be a valid absolute URI.");

            return uri;
        }
    }
}
