using Common.Enums;

namespace Common.Dto
{
    /// <summary>
    /// Class implementing the Rule Engine notification message DTO.
    /// </summary>
    /// <remarks>
    /// This DTO reflects the result of a specific rule engine rule.
    /// </remarks>
    public class RuleNotificationMessageDto
    {
        /// <summary>
        /// Property for the Device Id.
        /// </summary>
        /// <value>Gets or sets the Device Id</value>
        /// <remarks>
        /// The device a uniqueue numerical value.
        /// </remarks>
        public string EquipmentId { get; set; }

        /// <summary>
        /// Property for the notification message Type.
        /// </summary>
        /// <value>Gets or sets the notification message Type</value>
        /// <remarks>
        /// The value should be one of the following:
        /// <list type="bullet">
        /// <item><description><see cref="IngestMessageType.RuleInfo"/></description></item>
        /// <item><description><see cref="IngestMessageType.RuleRecommendation"/></description></item>
        /// <item><description><see cref="IngestMessageType.RuleWarning"/></description></item>
        /// <item><description><see cref="IngestMessageType.RuleError"/></description></item>
        /// </list>
        /// </remarks>
        public IngestMessageType NotificationType { get; set; }

        /// <summary>
        /// Property for an optional notification content.
        /// </summary>
        /// <value>Gets or sets the optional notification content</value>
        public string NotificationContent { get; set; }

        /// <summary>
        /// Property for the notification timestamp (UTC).
        /// </summary>
        /// <value>Gets or sets the notification timestamp (UTC)</value>
        public string NotificationTimestamp { get; set; }
    }
}
