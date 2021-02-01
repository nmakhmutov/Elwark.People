using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace People.Gateway.Infrastructure.Logger
{
    public class ElwarkSerilogFormatter : ITextFormatter
    {
        private readonly JsonValueFormatter _valueFormatter;

        public ElwarkSerilogFormatter() =>
            _valueFormatter = new JsonValueFormatter("$type");

        public void Format(LogEvent logEvent, TextWriter output)
        {
            Format(logEvent, output, _valueFormatter);
            output.WriteLine();
        }

        private static void Format(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
        {
            if (logEvent is null)
                throw new ArgumentNullException(nameof(logEvent));

            if (output is null)
                throw new ArgumentNullException(nameof(output));

            output.Write(@"{""timestamp"":""");
            output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
            output.Write(@""",""message"":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Render(logEvent.Properties), output);

            output.Write(@",""level"":""");
            output.Write(logEvent.Level);
            output.Write('"');

            if (logEvent.Exception != null)
            {
                output.Write(@",""exception"":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            foreach (var (key, value) in logEvent.Properties)
            {
                string str = key;
                if (str.Length > 0 && str[0] == '@')
                    str = "@" + str;
                output.Write(',');
                JsonValueFormatter.WriteQuotedJsonString(str, output);
                output.Write(':');
                valueFormatter.Format(value, output);
            }

            output.Write('}');
        }
    }
}