using System.Diagnostics;

namespace Presentation.Telemetry
{
    public static class OmniPortTelemetry
    {
        public const string ActivitySourceName = "OmniPort";

        public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    }
}
