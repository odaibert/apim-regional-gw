using System;
using System.Runtime.Serialization;

namespace Common.Dto
{
    /// <summary>
    /// Class implementing a Device Telemetry message DTO.
    /// </summary>
    /// <remarks>
    /// This class is used for serialization/deserialization of the Telemetry/NRT/Alert/Feature JSON messages.
    /// In case of a Device Feature Acknowledge, only tge proeprty <see cref="Feature"/> should be filled, together with Device Id and Timestamp.
    /// </remarks>
    public class DeviceTelemetryMessageDto
#if !WINDOWS_UWP
        : ICloneable
#endif
    {
        /// <summary>
        /// Property for the Device Id.
        /// </summary>
        /// <value>Gets or sets the Device Id</value>
        /// <remarks>
        /// The device is a uniqueue numerical value.
        /// </remarks>
        public long EquipmentId { get; set; }
        /// <summary>
        /// Property for the nested Values array, containing the various device parameters - <see cref="Values"/>.
        /// </summary>
        /// <value>Gets or sets the nested Values array</value>
        public Values Values { get; set; }
        /// <summary>
        /// Property for the Device Feature definition.
        /// </summary>
        /// <value>Gets or sets the Device Feature definition</value>
        /// <remarks>
        /// This property should only be filled for a Device Feature Acknowledge message initiated by the Device.
        /// </remarks>
        public DeviceFeatureDto Feature { get; set; }
        /// <summary>
        /// Property for the message timestamp.
        /// </summary>
        /// <value>Gets or sets the message timestamp</value>
        /// <remarks>
        /// The message timestamp is a UNIX timestamp (also known as UNIX epoch) in milliseconds.
        /// </remarks>
        public long TimeStamp { get; set; }

        /// <summary>
        /// Method for creating deep clone.
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            var values = (Values) Values.Clone();
            var feature = (DeviceFeatureDto) Feature.Clone();
            var retVal = (DeviceTelemetryMessageDto) MemberwiseClone();
            retVal.Values = values;
            retVal.Feature = feature;
            return retVal;
        }
    }

    /// <summary>
    /// Class implementing the device specific parameters contained in the Telemetry message.
    /// </summary>
    /// <remarks>
    /// The data of thie class will be handled as nested/sub object of the overall Telemetry message. - <see cref="DeviceTelemetryMessageDto"/>.
    /// </remarks>
    public class Values
#if !WINDOWS_UWP
        : ICloneable
#endif
    {
        public int? LiftingForceCrane { get; set; }
        public int? LiftingForceCraneMax { get; set; }
        public int? LiftingForceFlyjib { get; set; }
        public int? LiftingForceFlyjibMax { get; set; }
        public int? LengthOutreachCrane { get; set; }
        public int? LengthOutreachFlyjib { get; set; }
        public float? AngleValuesSlewing { get; set; }
        public float? AngleValuesMainBoom { get; set; }
        public float? AngleValuesKnuckleBoom { get; set; }
        public float? AngleValuesFlyJib { get; set; }
        public bool? OperationModeCrane { get; set; }
        public bool? OperationModePillar { get; set; }
        public bool? OperationModeManual { get; set; }
        public bool? OperationModeRemote { get; set; }
        public bool? EmergencyStop { get; set; }
        public object[] CurrentErrors { get; set; }
        /// <summary>
        /// Property for the device's operating hours.
        /// </summary>
        /// <value>Gets or sets the device's operating hours</value>
        /// <remarks>
        /// The value will be in seconds.
        /// </remarks>
        public int? OperatingHoursCrane { get; set; }
        /// <summary>
        /// Property for the device's usage consumption.
        /// </summary>
        /// <value>Gets or sets the device's usage consumption</value>
        /// <remarks>
        /// The value will be in the range of 15-25 liters.
        /// </remarks>
        public int? ConsumptionCrane { get; set; }
        /// <summary>
        /// Property for the truck's usage consumption.
        /// </summary>
        /// <value>Gets or sets the truck's usage consumption</value>
        /// <remarks>
        /// The value will be in the range of 30-45 liters.
        /// </remarks>
        public int? ConsumptionTruck { get; set; }
        /// <summary>
        /// Property for the current device's Geo Location: Longitude.
        /// </summary>
        /// <value>Gets or sets current device's Geo Location: Longitude</value>
        public double? Longitude { get; set; }
        /// <summary>
        /// Property for the current device's Geo Location: Latitude.
        /// </summary>
        /// <value>Gets or sets current device's Geo Location: Latitude</value>
        public double? Latitude { get; set; }

        /// <summary>
        /// Method for creating deep clone.
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }

}
