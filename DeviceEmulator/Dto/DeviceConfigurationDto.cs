using System;
using System.Collections.Generic;
#if !WINDOWS_UWP
using System.Device.Location;
#else
using Windows.Devices.Geolocation;
#endif
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Common.Dto;
using DeviceEmulator.Logic;

namespace DeviceEmulator.Dto
{
    /// <summary>
    /// Class implementing the Device configuration DTO.
    /// </summary>
    /// <remarks>
    /// A device is identified by its uniqueue serial number = Device Id.
    /// </remarks>
    public class DeviceConfigurationDto
    {
#region Member(s)
#endregion

#region Properties

        /// <summary>
        /// Property for the customer reference.
        /// </summary>
        /// <value>Gets or sets the customer reference</value>
        public string CustomerId { get; set; }

        /// <summary>
        /// Property for the unique Device Id.
        /// </summary>
        /// <value>Gets or sets the Device Id</value>
        /// <remarks>
        /// The device Id equals the uniqueue serial number.
        /// </remarks>
        public string DeviceId { get; set; }

        /// <summary>
        /// Property for the unique Device key for accessing the IoT Hub.
        /// </summary>
        /// <value>Gets or sets the Device key for accessing the IoT Hub</value>
        /// <remarks>
        /// </remarks>
        public string IoTHubDeviceKey { get; set; }

        /// <summary>
        /// Property for the unique Device X509 Certificate for accessing the IoT Hub.
        /// </summary>
        /// <value>Gets or sets the Device X509 Certificate for accessing the IoT Hub</value>
        /// <remarks>
        /// </remarks>
        public X509Certificate2 DeviceCertificate { get; set; }

        /// <summary>
        /// Property for the IoT Hub Host Name.
        /// </summary>
        /// <value>Gets or sets the IoT Hub Host Name</value>
        /// <remarks>
        /// </remarks>
        public string HostNameIoT { get; set; }

        /// <summary>
        /// Property for the Device Client.
        /// </summary>
        /// <value>Gets or sets the Device Client</value>
        /// <remarks>
        /// </remarks>
        public DeviceClient DeviceClient { get; set; }

        /// <summary>
        /// Property for the Device Time Tracking.
        /// </summary>
        /// <value>Gets or sets the Device Time Tracking</value>
        /// <remarks>
        /// The Device Time Tracking is used to simulate a useful time series when preparing Telemetry Messages.
        /// </remarks>
        public DateTime TimeTracking { get; set; }

        /// <summary>
        /// Property for the Device Geo Tracking.
        /// </summary>
        /// <value>Gets or sets the Device Geo Tracking</value>
        /// <remarks>
        /// The Device Geo Tracking is used to simulate a useful Geo LOcation series when preparing Telemetry Messages.
        /// </remarks>
#if !WINDOWS_UWP
        public List<GeoCoordinate> GeoTracking { get; set; }
#else
        public List<Geopoint> GeoTracking { get; set; }
#endif

        /// <summary>
        /// Property for the Device Telemetry Value Tracking.
        /// </summary>
        /// <value>Gets or sets the Device Telemetry Value Tracking</value>
        /// <remarks>
        /// The Device Telemetry Value Tracking is used to simulate a useful Telemetry Value series when preparing Telemetry Messages.
        /// As key, the name of the Telemetry Value proeprty is used.
        /// </remarks>
        public Dictionary<string, long> ValueTracking { get; set; }

        /// <summary>
        /// Property for the Device Feature Tracking.
        /// </summary>
        /// <value>Gets or sets the Device Feature Tracking</value>
        /// <remarks>
        /// </remarks>
        public List<DeviceFeatureDto> FeatureTracking { get; set; }

        /// <summary>
        /// Property for a Monitoring dispatch timer.
        /// </summary>
        /// <value>Gets or sets the Monitoring dispatch timer</value>
        public DeviceMonitoringTimer MonitoringDispatcher { get; set; }

        /// <summary>
        /// Property for the Device Client Operation Timeout (in millisecs).
        /// </summary>
        /// <value>Gets or sets the Device Client Operation Timeout (in millisecs)</value>
        /// <remarks>
        /// </remarks>
        public uint DeviceClientOperationTimeout { get; set; }

        /// <summary>
        /// Property for the Device Client Receive Timeout (in millisecs).
        /// </summary>
        /// <value>Gets or sets the Device Client Receive Timeout (in millisecs)</value>
        /// <remarks>
        /// </remarks>
        public uint DeviceClientReceiveTimeout { get; set; }

#endregion
    }
}
