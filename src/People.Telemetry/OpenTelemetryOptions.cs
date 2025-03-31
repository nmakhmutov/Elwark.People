using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace People.Telemetry;

public sealed class OpenTelemetryOptions
{
    public string AppName { get; set; } = string.Empty;

    public Action<MeterProviderBuilder> Metrics { get; set; } = _ => { };

    public Action<TracerProviderBuilder> Traces { get; set; } = _ => { };
}
