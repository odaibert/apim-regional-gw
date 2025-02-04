
namespace Common.Enums
{
    /// <summary>
    /// Enumeration defining the supported Message Types.
    /// </summary>
    /// <remarks>
    /// The type values include single/batch Telemetry/NRT data,a s well as device alerts.
    /// </remarks>
    public enum IngestMessageType
    {
        // Unknown message type
        Unknown = 0,
        // Near Real Time Telemetry
        NrtTelemetry = 1,
        // Batch = Package Telemetry (GZiped)
        BatchTelemetry = 2,
        // Devive Alert
        Alert = 3,
        // Feature en/disable
        FeatureNotification = 4,
        // Rule Notification: Info
        RuleInfo = 5,
        // Rule Notification: Info
        RuleRecommendation = 6,
        // Rule Notification: Warning
        RuleWarning = 7,
        // Rule Notification: Error
        RuleError = 8,
    }

    /// <summary>
    /// Enumeration defining the supported Logging Entry Types.
    /// </summary>
    /// <remarks>
    ///  </remarks>
    public enum LogEntryType
    {
        Error = 0,
        Warning = 1,
        Info = 3
    }
}
