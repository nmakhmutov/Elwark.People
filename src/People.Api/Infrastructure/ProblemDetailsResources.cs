using System.Globalization;
using System.Resources;
using People.Api.Resources;

namespace People.Api.Infrastructure;

internal static class ProblemDetailsResources
{
    private static readonly ResourceManager ErrorsResources =
        new($"{typeof(Errors).Namespace}.Errors", typeof(Errors).Assembly);

    public static string GetString(string key, params ReadOnlySpan<object> args)
    {
        var template = TryGetString(key)
            ?? throw new InvalidOperationException($"Missing resource key '{key}'. Add it to Resources");

        if (args.Length == 0)
            return template;

        try
        {
            return string.Format(CultureInfo.CurrentUICulture, template, args);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Resource '{key}' format string does not match the provided arguments ({args.Length} args).", ex);
        }
    }

    public static string? TryGetString(string key) =>
        ErrorsResources.GetString(key, CultureInfo.CurrentUICulture)
        ?? ErrorsResources.GetString(key, CultureInfo.InvariantCulture);
}
