
namespace Common.Enums
{
    /// <summary>
    /// Enumeration defining the supported C2D Command Types.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public enum C2DCommandType
    {
        // Unknown message type
        Unknown = 0,
        // Request single Telemetrie Message from Devive
        RequestSingleTelemetry = 1,
        // Request Telemetrie Message Package from Devive
        RequestTelemetryPackage = 2,
        // Feature en/disabling
        EnDisableFeature = 3,
        // Start Device Monitoring (stream of single Telemetry Messages until stop)
        StartMonitoring = 4,
        // Stop Device Monitoring (stream of single Telemetry Messages until stop)
        StopMonitoring = 5,
        // Feature usage
        FeatureUsage = 6,
    }
}
