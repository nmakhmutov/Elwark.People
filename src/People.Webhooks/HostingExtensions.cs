using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

public static class HostingExtensions
{
    public static IHostApplicationBuilder AddSerilog(this IHostApplicationBuilder builder, string appName) =>
        builder.AddSerilog(appName, _ => { });

    public static IHostApplicationBuilder AddSerilog(
        this IHostApplicationBuilder builder,
        string appName,
        Action<LoggerConfiguration> configureLogger
    )
    {
        builder.Services
            .AddSerilog((_, configuration) =>
            {
                configuration
                    .Enrich.WithProperty("ApplicationName", appName)
                    .Enrich.FromLogContext()
                    .ReadFrom.Configuration(builder.Configuration);

                if (builder.Environment.IsDevelopment())
                    configuration.WriteTo.Console(outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}"
                    );
                else
                    configuration.WriteTo.Console(new ElwarkJsonFormatter());

                configureLogger(configuration);
            });

        return builder;
    }
}

internal sealed class ElwarkJsonFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _valueFormatter;

    public ElwarkJsonFormatter(JsonValueFormatter? valueFormatter = null) =>
        _valueFormatter = valueFormatter ?? new JsonValueFormatter("$type");

    public void Format(LogEvent logEvent, TextWriter output)
    {
        FormatEvent(logEvent, output, _valueFormatter);
        output.WriteLine();
    }

    private static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(valueFormatter);

        output.Write('{');
        output.Write($"\"@t\":\"{logEvent.Timestamp.UtcDateTime:O}\",\"@l\":\"{logEvent.Level}\"");
        output.Write(",\"@m\":");

        var message = logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture);
        JsonValueFormatter.WriteQuotedJsonString(message, output);

        if (logEvent.Exception != null)
        {
            output.Write(",\"@x\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        foreach (var property in logEvent.Properties)
        {
            var name = property.Key;
            if (name.Length > 0 && name[0] == '@')
                name = '@' + name;

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        output.Write('}');
    }
}
