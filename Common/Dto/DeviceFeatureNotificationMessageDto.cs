using System;
using Common.Enums;

namespace Common.Dto
{
    /// <summary>
    /// Class implementing the Feature en/disable notification DTO.
    /// </summary>
    /// <remarks>
    /// This DTO reflects the Feature data from the original C2D Command, sent from the UI.
    /// </remarks>
    public class DeviceFeatureNotificationMessageDto
    {
        /// <summary>
        /// Property for the Device Id.
        /// </summary>
        /// <value>Gets or sets the Device Id</value>
        /// <remarks>
        /// The device Id is a uniqueue numerical value.
        /// </remarks>
        public long EquipmentId { get; set; }

        /// <summary>
        /// Property for the notification message Type.
        /// </summary>
        /// <value>Gets or sets the notification message Type</value>
        /// <remarks>
        /// The value should be of type <see cref="IngestMessageType.FeatureNotification"/>.
        /// </remarks>
        public IngestMessageType NotificationType { get; set; }

        /// <summary>
        /// Property for an optional notification comment.
        /// </summary>
        /// <value>Gets or sets the optional notification comment</value>
        public string NotificationComment { get; set; }

        /// <summary>
        /// Property for the notification timestamp (UTC).
        /// </summary>
        /// <value>Gets or sets the notification timestamp (UTC)</value>
        public DateTime NotificationTimestamp { get; set; }

        /// <summary>
        /// Property for the feature deatils, this notification is about.
        /// </summary>
        /// <value>Gets or sets the feature deatils</value>
        /// <remarks>
        /// This property should contain the device feature details of the original C2D Command of type <see cref="C2DCommandType.EnDisableFeature"/>
        /// </remarks>
        public DeviceFeatureDto Feature { get; set; }
    }
}
