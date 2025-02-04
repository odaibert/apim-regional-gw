
using Common.Enums;

namespace Common.Dto
{
    /// <summary>
    /// Class implementing the Custom Command DTO used for C2D Commands.
    /// </summary>
    /// <remarks>
    /// This DTO reflects the C2D Command data, sent from the UI.
    /// </remarks>
    public class DeviceCommandMessageDto
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
        /// Property for the C2D Command Type.
        /// </summary>
        /// <value>Gets or sets the C2D Command Type</value>
        public string CommandType { get; set; }

        /// <summary>
        /// Property for the C2D Command Description.
        /// </summary>
        /// <value>Gets or sets the C2D Command Description</value>
        public string CommandDescription { get; set; }

        /// <summary>
        /// Property for the C2D Command Feature.
        /// </summary>
        /// <value>Gets or sets the C2D Command Feauture</value>
        /// <remarks>
        /// This property should contain the device feature details if the command is of type <see cref="C2DCommandType.EnDisableFeature"/>
        /// </remarks>
        public DeviceFeatureDto Feature { get; set; }
    }
}
