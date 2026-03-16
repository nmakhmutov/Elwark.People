namespace People.Api.Infrastructure;

internal static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public string GetString(string key)
        {
            var value = configuration[key];

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Configuration value '{key}' is required.");

            return value;
        }

        public Uri GetUri(string key)
        {
            var value = configuration.GetString(key);

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Configuration value '{key}' must be a valid absolute URI.");

            return uri;
        }

        public Uri GetUri(string key, string path)
        {
            var url = configuration.GetUri(key);

            return new Uri(url, path);
        }
    }
}
