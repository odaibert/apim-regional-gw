using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Common.Dto;
using Common.Enums;
using DeviceEmulator.Dto;

namespace DeviceEmulator.Logic
{
    /// <summary>
    /// Interface defining the supported logic of the Device Emulator. 
    /// </summary>
    public interface IEmulatorLogic
    {
        #region Properties

        /// <summary>
        /// Property for the Device Emulator configuration data.
        /// </summary>
        /// <value>Gets or sts the Device Emulator configuration data</value>
        ConfigurationDto Configuration { get; set; }

        /// <summary>
        /// Property for the device C2D Command dispatcher dictionary.
        /// </summary>
        /// <value>Gets or sets the C2D Command dispatcher dictionary</value>
        /// <remarks>
        /// This dictionary is used to keep an instance of the <see cref="C2DCommandTimer"/> per each supported Device.
        /// </remarks>
        Dictionary<string, C2DCommandTimer> DeviceC2DDispatcher { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Helper method for retrieving all sample device Telemetry files.
        /// </summary>
        /// <param name="folderName">Name of the folder to use as sample sub-folder for current runtime directory</param>
        /// <returns>List of <see cref="TelemetrySampleFileDto"/> objects representing the files and some metadata</returns>
        /// <remarks>
        /// The sample files might be of different types:
        /// <list type="bullet">
        /// <item><term>filename contains &quot;Telemetry&quot;</term><description>either single Telemetry or Package</description></item>
        /// <item><term>filename contains &quot;Alert&quot;</term><description>either single Alert</description></item>
        /// <item><term>filename end with &quot;.gz&quot;</term><description>GZiped Telemetry Package</description></item>
        /// </list>
        /// </remarks>
#if !WINDOWS_UWP
        List<TelemetrySampleFileDto> GetSampleDataFileNames(string folderName);
#else
        Task<List<TelemetrySampleFileDto>> GetSampleDataFileNames(string folderName);
#endif

        /// <summary>
        /// Method for igesting Telemetry/NRT/Alert message into IoT Hub.
        /// </summary>
        /// <param name="telemetryIngestFile">Telemetry/NRT/Alert sample file details</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <param name="deviceCommand">Optional C2D Command details</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// </remarks>
        Task<bool> IngestTelemetryAsync(TelemetrySampleFileDto telemetryIngestFile, DeviceConfigurationDto deviceConfigurationDto, DeviceCommandMessageDto deviceCommand = null);

        /// <summary>
        /// Method for sending out a single Telemetry/NRT message to the IoT Hub.
        /// </summary>
        /// <param name="telemetryMessage">Single Telemetry/NRT message content</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <param name="messageType">Optional message type to set as IoT Hub message property</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// </remarks>
        Task<bool> IngestSingleTelemetryAsync(DeviceTelemetryMessageDto telemetryMessage, DeviceConfigurationDto deviceConfigurationDto, IngestMessageType messageType = IngestMessageType.Unknown);

        /// <summary>
        /// Method for handling a batch of single Telemetry/NRT messages.
        /// </summary>
        /// <param name="telemetryMessageBatch">List of single Telemetry Alert/NRT messages</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// This method DOES not add a message type proeprty to the IoT Hub messages to be sent out,
        /// it uses the batch send API.
        /// </remarks>
        Task<bool> IngestTelemetryBatchAsync(List<Message> telemetryMessageBatch, DeviceConfigurationDto deviceConfigurationDto);

#if !WINDOWS_UWP
        /// <summary>
        /// Method for handling a Gziped Telemetry message package.
        /// </summary>
        /// <param name="messageList">List of Telemetry messages</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// The provided list of Telemetry messages has to be GZiped before uploading to the IoT Hub.
        /// </remarks>
        Task<bool> IngestPackageTelemetryAsync(List<DeviceTelemetryMessageDto> messageList, DeviceConfigurationDto deviceConfigurationDto);
#endif

        /// <summary>
        /// Helper method for simulating a specific Alert for a Device.
        /// </summary>
        /// <param name="statusIndex">Index to internal config of supported Alerts</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// The sample files might be of different types:
        /// <list type="bullet">
        /// <item><term>filename contains &quot;Telemetry&quot;</term><description>either single Telemetry or Package</description></item>
        /// <item><term>filename contains &quot;Alert&quot;</term><description>either single Alert</description></item>
        /// <item><term>filename end with &quot;.gz&quot;</term><description>GZiped Telemetry Package</description></item>
        /// </list>
        /// </remarks>
        Task<bool> IngestAlertAsync(int statusIndex, DeviceConfigurationDto deviceConfigurationDto);

        /// <summary>
        /// Helper method for simulating a specific Feature Acknowledge for a Device.
        /// </summary>
        /// <param name="statusIndex">Index to internal config of supported Alerts</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <param name="deviceCommand">C2D Command for feature en/disable</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// For the Telemetry message skeleton, the Alert sample file with given index (<param ref="statusIndex"/>) is used.
        /// </remarks>
        Task<bool> IngestFeatureAcknowledgetAsync(int statusIndex, DeviceConfigurationDto deviceConfigurationDto, DeviceCommandMessageDto deviceCommand);

        /// <summary>
        /// Helper method for simulating a specific Feature Usage for a Device.
        /// </summary>
        /// <param name="statusIndex">Index to internal config of supported Alerts</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <param name="deviceFeature">Device feature details</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// For the Telemetry message skeleton, the Alert sample file with given index (<param ref="statusIndex"/>) is used.
        /// </remarks>
        Task<bool> ProcessFeatureUsagetAsync(int statusIndex, DeviceConfigurationDto deviceConfigurationDto, DeviceFeatureDto deviceFeature);

        /// <summary>
        /// Method for checking for C2D Commands on the IoT HUb (device queue).
        /// </summary>
        /// <param name="deviceConfigurationDto">Config details of selected device</param>
        /// <returns>C2D Command Details</returns>
        /// <remarks>
        /// Method tries to read from the IoT Hub device queue and in case there is a C2D Message, fires an event.
        /// </remarks>
        Task<DeviceCommandMessageDto> CheckForCommandAsync(DeviceConfigurationDto deviceConfigurationDto);
        /// <summary>
        /// Method for processing a feature en/disable request triggered by specific C2D Command.
        /// </summary>
        /// <param name="deviceConfiguration">Device configuration details</param>
        /// <param name="deviceCommand">Original C2D Command Message</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        Task<bool> ProcessFeatureEnDisable(DeviceConfigurationDto deviceConfiguration, DeviceCommandMessageDto deviceCommand);

        /// <summary>
        /// Method for retrieving the device features based on the Twin data.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device configuration detials</param>
        /// <returns>List of device features</returns>
        /// <remarks>
        /// </remarks>
        Task<List<DeviceFeatureDto>> GetDeviceFeaturesFromTwin(DeviceConfigurationDto deviceConfigurationDto);

        /// <summary>
        /// Method for handling the Device Monitoring functionality, triggered by a dispatch timer.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device configuration, for which the monitoring has to be handled</param>
        /// <returns><see langword="true"/> if successfully sent out Monitoring Telemetry, otherwise <see langword="false"/></returns>
        /// <remarks>
        /// If the Devive Monitoring dispatch timer is triggerd, a single Telemetry message will be sent out for the Device to the IoT Hub.
        /// </remarks>
        Task<bool> SendMonitoringTelemetryAsync(DeviceConfigurationDto deviceConfigurationDto);

        /// <summary>
        /// Helper method for creating a IoT Hub Device Client for given Transport Type and based on the Device details.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device config details</param>
        /// <param name="transportType">Optional IoT Hub Transport Type, <see cref="TransportType.Mqtt"/> being the default</param>
        /// <param name="openImmediate">Flag indicating, wheter the Device Cleint should be opened explicitly directly afer creating</param>
        /// <returns>Device Client, or <see langword="null"/> if neither certificate, nor key are available for Device</returns>
        /// <remarks>
        /// If the provided Device config has certificate defined, this will be used for creating the Device Client.
        /// Otherwise the defined key will be used.
        /// </remarks>
        Task<DeviceClient> GetDeviceClient(DeviceConfigurationDto deviceConfigurationDto, TransportType transportType = TransportType.Mqtt, bool openImmediate = false);

#endregion

#region Event/Delegate

        /// <summary>
        /// Event for C2D Command handling.
        /// </summary>
        event EmulatorLogic.C2DCommandEventHandler C2DCommandArrived;
        /// <summary>
        /// Event for UI status update handling.
        /// </summary>
        event EmulatorLogic.StatusUpdateChangeEventHandler StatusUpdateChange;

#endregion
    }
}
