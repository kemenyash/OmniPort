using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Web.Telemetry
{
    public static class OpenTelemetryConfiguration
    {
        public static IServiceCollection AddOmniPortOpenTelemetry(
            this IServiceCollection services,
            string serviceName,
            bool isConsoleExporterEnabled = false)
        {
            services
                .AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder.SetSampler(new AlwaysOnSampler());
                    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                        serviceName,
                        serviceVersion: "1.0.0",
                        autoGenerateServiceInstanceId: false,
                        serviceInstanceId: Environment.MachineName));

                    builder.AddSource(OmniPortTelemetry.ActivitySourceName);
                    builder.AddAspNetCoreInstrumentation(ConfigureAspNetCoreInstrumentation);
                    builder.AddHttpClientInstrumentation(ConfigureHttpClientInstrumentation);
                    builder.AddEntityFrameworkCoreInstrumentation();

                    if (isConsoleExporterEnabled)
                    {
                        builder.AddConsoleExporter();
                    }

                    if (!Debugger.IsAttached)
                    {
                        builder.AddOtlpExporter(ConfigureOtlpExporter);
                    }
                })
                .WithMetrics(builder =>
                {
                    builder.AddAspNetCoreInstrumentation();
                    builder.AddHttpClientInstrumentation();
                    builder.AddRuntimeInstrumentation();
                    builder.AddMeter(OmniPortTelemetry.MeterName);

                    if (isConsoleExporterEnabled)
                    {
                        builder.AddConsoleExporter();
                    }

                    builder.AddPrometheusExporter(options =>
                    {
                        options.ScrapeResponseCacheDurationMilliseconds = 0;
                        options.ScrapeEndpointPath = "/metrics";
                    });
                });

            return services;
        }

        public static IApplicationBuilder PassTraceIdToResponse(this IApplicationBuilder app)
        {
            app.Use((httpContext, next) =>
            {
                httpContext.Response.Headers.TryAdd(
                    "x-trace-id",
                    Activity.Current?.TraceId.ToString() ?? string.Empty);

                return next();
            });

            return app;
        }

        private static void ConfigureAspNetCoreInstrumentation(AspNetCoreTraceInstrumentationOptions options)
        {
            options.RecordException = true;
            options.Filter = httpContext =>
            {
                var path = httpContext.Request.Path.Value;
                if (string.Equals(path, "/metrics", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(path, "/favicon.ico", StringComparison.OrdinalIgnoreCase)) return false;
                if (path?.Contains("_framework", StringComparison.OrdinalIgnoreCase) == true) return false;
                return true;
            };
        }

        private static void ConfigureHttpClientInstrumentation(HttpClientTraceInstrumentationOptions options)
        {
            options.RecordException = true;
        }

        private static void ConfigureOtlpExporter(OtlpExporterOptions options)
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
            options.ExportProcessorType = ExportProcessorType.Batch;
        }
    }
}
