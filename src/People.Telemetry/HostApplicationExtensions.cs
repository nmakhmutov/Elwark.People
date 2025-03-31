using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using People.Telemetry;
using Serilog;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

public static class HostApplicationExtensions
{
    public static IHostApplicationBuilder AddOpenTelemetry(this IHostApplicationBuilder builder, string appName) =>
        builder.AddOpenTelemetry(options => options.AppName = appName);

    public static IHostApplicationBuilder AddOpenTelemetry(
        this IHostApplicationBuilder builder,
        Action<OpenTelemetryOptions> configureOptions
    )
    {
        var configuration = new OpenTelemetryOptions();
        configureOptions.Invoke(configuration);

        if (string.IsNullOrEmpty(configuration.AppName))
            throw new InvalidOperationException("Open telemetry AppName has not been set");

        builder.Logging
            .AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            });

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(configuration.AppName)
                .AddContainerDetector()
                .AddHostDetector()
            )
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                metrics.AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
                });

                if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                    metrics.AddOtlpExporter();

                configuration.Metrics(metrics);
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                    tracing.SetSampler<AlwaysOnSampler>();

                tracing.AddAspNetCoreInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                    tracing.AddOtlpExporter();

                configuration.Traces(tracing);
            });

        return builder;
    }

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
