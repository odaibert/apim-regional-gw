
using System;
using Common.Enums;

namespace Common.Dto
{
    /// <summary>
    /// Class implementing a Device Feature Set DTO.
    /// </summary>
    public class DeviceFeatureSetDto
    {
        /// <summary>
        /// Property for the feature set of a device.
        /// </summary>
        /// <value>Gets or sets the device's featre set</value>
        public DeviceFeatureDto[] DeviceFeatureSet { get; set; }
    }

    /// <summary>
    /// Class implementing a Device Feature DTO.
    /// </summary>
    public class DeviceFeatureDto
#if !WINDOWS_UWP
        : ICloneable
#endif
    {
        /// <summary>
        /// Property for the feature Id.
        /// </summary>
        /// <value>Gets or sets the feature Id</value>
        public long FeatureId { get; set; }
        /// <summary>
        /// Property for the feature name.
        /// </summary>
        /// <value>Gets or sets the feature name</value>
        public string FeatureName { get; set; }
        /// <summary>
        /// Property for the feature en/disabled flag.
        /// </summary>
        /// <value>Gets or sets the feature en/disabled flag</value>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// Property for the feature config Id.
        /// </summary>
        /// <value>Gets or sets the feature config Id</value>
        /// <remarks>
        /// The config Id is used to detect a change (before/after) of the configuration of the feature.
        /// </remarks>
        public string ConfigId { get; set; }
        /// <summary>
        /// Property for the feature update status.
        /// </summary>
        /// <value>Gets or sets the feature update status</value>
        public string Status { get; set; }
        /// <summary>
        /// Property for the last modified timestamp.
        /// </summary>
        /// <value>Gets or sets the last modified timestamp</value>
        public DateTime LastModified { get; set; }
        /// <summary>
        /// Property for the validation count.
        /// </summary>
        /// <value>Gets or sets the validation count</value>
        /// <remarks>
        /// Whenever the DeviceFeatureChecker Service validates the desired property in the Device Twin, it will increase this counter.
        /// </remarks>
        public int ValidationCount { get; set; }
        /// <summary>
        /// Property for the credit count in trial mode.
        /// </summary>
        /// <value>Gets or sets the credit count</value>
        public int CreditCount { get; set; }
        /// <summary>
        /// Property for the current Feature Usage status.
        /// </summary>
        /// <value>Gets or sets the current Feature Usage status</value>
        public FeatureUsageStatus UsageStatus { get; set; }

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


