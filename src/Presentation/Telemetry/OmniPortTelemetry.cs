using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Presentation.Telemetry
{
    public static class OmniPortTelemetry
    {
        public const string ActivitySourceName = "OmniPort";
        public const string MeterName = "OmniPort";

        public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
        public static readonly Meter Meter = new(MeterName, "1.0.0");

        public static readonly Counter<long> TransformationsStarted =
            Meter.CreateCounter<long>("omniport_transformations_started", description: "Number of transformations started.");

        public static readonly Counter<long> TransformationsFailed =
            Meter.CreateCounter<long>("omniport_transformations_failed", description: "Number of transformations failed.");

        public static readonly Histogram<double> TransformationDuration =
            Meter.CreateHistogram<double>("omniport_transformation_duration", unit: "ms", description: "Transformation duration.");

        public static readonly Counter<long> WatchedUrlChecks =
            Meter.CreateCounter<long>("omniport_watched_url_checks", description: "Number of watched URL checks.");
    }
}
