using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace People.Telemetry;

public sealed class ElwarkJsonFormatter : ITextFormatter
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

        output.Write(
            $"""
             "@t":"{logEvent.Timestamp.UtcDateTime:O}","@l":"{logEvent.Level}"
             """
        );

        if (logEvent.TraceId.HasValue)
            output.Write(
                $"""
                 ,"@tr":"{logEvent.TraceId.Value.ToHexString()}"
                 """
            );

        if (logEvent.SpanId.HasValue)
            output.Write(
                $"""
                 ,"@sp":"{logEvent.SpanId.Value.ToHexString()}"
                 """
            );

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
                name = '@' + name; // Escape first '@' by doubling

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        output.Write('}');
    }
}
