
namespace Common.Enums
{
    /// <summary>
    /// Enumeration defining the Device Feature status vales.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public enum FeatureUsageStatus
    {
        // Unknown message type
        Unknown = 0,
        // Feature executed
        FeatureExecuted = 1,
        // Feature enabled, can be used
        FeatureActive = 2,
        // Feature disabled, cannot be used
        FeatureNotActive = 3,
        // Feature blocked in general
        FeatureBlocked = 4,
        // Feature CreditCount exceeded (= 0)
        FeatureCountExeeded = 5,
    }
}
