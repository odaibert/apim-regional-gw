using System;
using System.Collections.Generic;
#if !WINDOWS_UWP
using System.Device.Location;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json.Converters;
#else
using System.Runtime.Serialization.Json;
using Windows.Data.Json;
using Windows.Devices.Geolocation;
using Windows.Storage;
#endif
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static System.String;
// Azure
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// Own usings
using Common.Dto;
using Common.Enums;
using Common.Logging;
#if !WINDOWS_UWP
using Common.Utilities;
#endif
using DeviceEmulator.Dto;
using DeviceEmulator.Logic.Event;

namespace DeviceEmulator.Logic
{
    /// <summary>
    /// Class implementing the Device Emulator Business Logic.
    /// </summary>
    public class EmulatorLogic : IEmulatorLogic
    {
        #region Event(s)/Delegate(s)

        /// <summary>
        /// Delegate for C2D Command handling.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        public delegate Task C2DCommandEventHandler(object sender, C2DCommandEventArgs e);
        /// <summary>
        /// Event for C2D Command handling.
        /// </summary>
        public event C2DCommandEventHandler C2DCommandArrived;

        /// <summary>
        /// Delegate for UI status update handling.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        public delegate void StatusUpdateChangeEventHandler(object sender, StatusChangeEventArgs e);
        /// <summary>
        /// Event for UI status update handling.
        /// </summary>
        public event StatusUpdateChangeEventHandler StatusUpdateChange;

        #endregion

        #region Members

        private ConfigurationDto _configuration;

        #endregion

        #region Properties

        /// <summary>
        /// Property for the Device Emulator configuration data.
        /// </summary>
        /// <value>Gets or sts the Device Emulator configuration data</value>
        public ConfigurationDto Configuration
        {
            get { return _configuration; }
            set { _configuration = value; }
        }

        /// <summary>
        /// Property for the device C2D Command dispatcher dictionary.
        /// </summary>
        /// <value>Gets or sets the C2D Command dispatcher dictionary</value>
        /// <remarks>
        /// This dictionary is used to keep an instance of the <see cref="C2DCommandTimer"/> per each supported Device.
        /// </remarks>
        public Dictionary<string, C2DCommandTimer> DeviceC2DDispatcher { get; set; } = new Dictionary<string, C2DCommandTimer>();

        #endregion

        #region Public Methods

        #region General functionality

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
        public List<TelemetrySampleFileDto> GetSampleDataFileNames(string folderName)
#else
        public async Task<List<TelemetrySampleFileDto>> GetSampleDataFileNames(string folderName)
#endif
        {
            try
            {
#if !WINDOWS_UWP
                string telemetryFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
                return Directory.GetFiles(telemetryFolder).Select(item => new TelemetrySampleFileDto
                {
                    FullPath = item,
                    FileName = Path.GetFileName(item),
                    IsZipped = item.EndsWith(".gz"),
                    MessageType = item.ToLower().Contains("telemetry") ? (item.EndsWith(".gz") ? IngestMessageType.BatchTelemetry : IngestMessageType.NrtTelemetry) : item.ToLower().Contains("alert") ? IngestMessageType.Alert : IngestMessageType.Unknown,
                }).ToList();
#else
                var sampleFileList = new List<TelemetrySampleFileDto>();
                var appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var subFolder = await appFolder.GetFolderAsync($"Assets\\SampleFiles");
                var fileList = await subFolder.GetFilesAsync();

                foreach (var file in fileList)
                {
                    var messageType = file.Name.ToLower().Contains("telemetry")
                        ? (file.Name.EndsWith(".gz") ? IngestMessageType.BatchTelemetry : IngestMessageType.NrtTelemetry)
                        : file.Name.ToLower().Contains("alert") ? IngestMessageType.Alert : IngestMessageType.Unknown;
                    // Telemetry Package = Batch currently not supported
                    if (messageType != IngestMessageType.BatchTelemetry)
                        sampleFileList.Add(new TelemetrySampleFileDto
                        {
                            FullPath = file.Path,
                            FileName = file.Name,
                            IsZipped = file.Name.EndsWith(".gz"),
                            MessageType = messageType,
                        });
                }
                return sampleFileList;
#endif
            }
            catch (Exception)
            {
                //TODO: Add Error Tracking
                return new List<TelemetrySampleFileDto>();
            }
        }

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
        public async Task<DeviceClient> GetDeviceClient(DeviceConfigurationDto deviceConfigurationDto, TransportType transportType = TransportType.Mqtt, bool openImmediate = false)
        {
            // Init
            var deviceClient = deviceConfigurationDto.DeviceClient;

            if (deviceClient == null)
                try
                {
#if !WINDOWS_UWP
// Preference is on Device Certificate
                    if (deviceConfigurationDto.DeviceCertificate != null)
                    {
                        // Create new Device Client and send out message: using symmetric key
                        deviceClient = DeviceClient.Create(deviceConfigurationDto.HostNameIoT, new DeviceAuthenticationWithX509Certificate(deviceConfigurationDto.DeviceId, deviceConfigurationDto.DeviceCertificate), transportType);
                    }
                    else if (!IsNullOrEmpty(deviceConfigurationDto.IoTHubDeviceKey))
#else
                    if (!IsNullOrEmpty(deviceConfigurationDto.IoTHubDeviceKey))
#endif
                        if (deviceConfigurationDto.IoTHubDeviceKey.ToLower().Contains("sharedaccesssignature"))
                            deviceClient = DeviceClient.Create(deviceConfigurationDto.HostNameIoT, AuthenticationMethodFactory.CreateAuthenticationWithToken(deviceConfigurationDto.DeviceId, deviceConfigurationDto.IoTHubDeviceKey), transportType);
                        else
                            deviceClient = DeviceClient.Create(deviceConfigurationDto.HostNameIoT, new DeviceAuthenticationWithRegistrySymmetricKey(deviceConfigurationDto.DeviceId, deviceConfigurationDto.IoTHubDeviceKey), transportType);

                    // If we have a valid Device Client reference and the flag for opening was set, lets do it
                    if (deviceClient != null)
                    {
                        // Set configured timeout value
                        deviceClient.OperationTimeoutInMilliseconds = deviceConfigurationDto.DeviceClientOperationTimeout;

                        if (openImmediate)
                            await deviceClient.OpenAsync();
                    }
                }
                catch (Exception)
                {
                    deviceClient = null;
                }

            return deviceClient;
        }

        #endregion

        #region Telemetry Data handling

        /// <summary>
        /// Method for igesting Telemetry/NRT/Alert message into IoT Hub.
        /// </summary>
        /// <param name="telemetryIngestFile">Telemetry/NRT/Alert sample file details</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <param name="deviceCommand">Optional C2D Command details in case of Feature Notification, otherwise <see langword="null"/></param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// </remarks>
        public async Task<bool> IngestTelemetryAsync(TelemetrySampleFileDto telemetryIngestFile, DeviceConfigurationDto deviceConfigurationDto, DeviceCommandMessageDto deviceCommand = null)
        {
            // Mandatory checking
            if (IsNullOrEmpty(telemetryIngestFile?.FullPath) || IsNullOrEmpty(deviceConfigurationDto?.DeviceId))
            {
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Missing Ingest Filename or missing Device Id!"));
                return false;
            }

            // Check file existence
            if (!File.Exists(telemetryIngestFile.FullPath))
            {
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Missing Ingest File!"));
                return false;
            }

            // Init some stuff
            var isSuccess = true;
            List<DeviceTelemetryMessageDto> messageList;
#if !WINDOWS_UWP
            GeoCoordinate geoCoordinate = _configuration.DeviceEmulatorPosition?.Location;
#else
            var geoCoordinate = _configuration.DeviceEmulatorPosition?.Coordinate.Point;
#endif
            var isFirstMessage = true;

            // Based on the type of message file, do the necessary
            switch (telemetryIngestFile.MessageType)
            {
                case IngestMessageType.BatchTelemetry:
#if !WINDOWS_UWP
                    // Obviously we have to handle a Telemetry package, which is GZiped
                    messageList = await ReadTelemetryMessagesFromGZipAsync(telemetryIngestFile);

                    UpdateDevieTimeTracking(deviceConfigurationDto);

                    // Now lets process the messages
                    foreach (var singleMessage in messageList)
                    {
                        // Update message to make it a new one: device Id, UNIX time (epoch) and some other - use current time -15min
                        UpdateTelemetryMessage(singleMessage, deviceConfigurationDto, ref geoCoordinate, isFirstMessage);
                        isFirstMessage = false;
                    }

                    // Now lets ingest the package, but as GZip again
                    isSuccess = await IngestPackageTelemetryAsync(messageList, deviceConfigurationDto);
#else
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Telemetry Package ingest currently not supported in UWP!"));

#endif
                    break;
                case IngestMessageType.NrtTelemetry:
                    // Obviously we have to handle a single or bunch of single Telemetry (NRT) messages
                    messageList = await ReadTelemetryMessagesFromTextAsync(telemetryIngestFile);

                    // Lets get the count of messages read in
                    var messageCount = messageList.Count;
                    var iotHubMessages = new List<Message>(messageCount);

                    // Update timestamp tracking for device
                    UpdateDevieTimeTracking(deviceConfigurationDto);

                    // Now lets enhance the messages with an updated timestamp and some random data
                    foreach (var singleMessage in messageList)
                    {
                        // Update message to make it a new one: device Id, UNIX time (epoch) and some other - use current time -15min
                        UpdateTelemetryMessage(singleMessage, deviceConfigurationDto, ref geoCoordinate, isFirstMessage);
                        isFirstMessage = false;

                        // Add to batch if more than one Telemetry message
                        if (messageCount > 1)
                            iotHubMessages.Add(SerializeTelemetryMessage(singleMessage, IngestMessageType.NrtTelemetry));
                    }

                    if (messageCount == 1)
                        isSuccess &= await IngestSingleTelemetryAsync(messageList[0], deviceConfigurationDto, IngestMessageType.NrtTelemetry);
                    else if (messageCount > 1)
                        isSuccess &= await IngestTelemetryBatchAsync(iotHubMessages, deviceConfigurationDto);

                    break;
                case IngestMessageType.Alert:
                    // Obviously we have to handle a bunch of single Telemetry (Alert) messages
                    messageList = await ReadTelemetryMessagesFromTextAsync(telemetryIngestFile);

                    UpdateDevieTimeTracking(deviceConfigurationDto);

                    // Now lets process the messages
                    foreach (var singleMessage in messageList)
                    {
                        // Update message to make it a new one: device Id, UNIX time (epoch) and some other - use current time -15min
                        UpdateTelemetryMessage(singleMessage, deviceConfigurationDto, ref geoCoordinate, isFirstMessage);
                        isFirstMessage = false;

                        // Now lets ingest the single message
                        isSuccess &= await IngestSingleTelemetryAsync(singleMessage, deviceConfigurationDto, IngestMessageType.Alert);
                    }

                    break;
                case IngestMessageType.FeatureNotification:
                    // Obviously we have to handle a bunch of single Telemetry (Feature Notification) messages
                    messageList = await ReadTelemetryMessagesFromTextAsync(telemetryIngestFile);

                    UpdateDevieTimeTracking(deviceConfigurationDto);

                    // Now lets process the messages
                    foreach (var singleMessage in messageList)
                    {
                        // Update message to make it a new one: device Id, UNIX time (epoch) and some other - use current time -15min
                        UpdateFeatureNotificationMessage(singleMessage, deviceConfigurationDto, ref geoCoordinate, deviceCommand, isFirstMessage);
                        isFirstMessage = false;

                        // Now lets ingest the single message
                        isSuccess &= await IngestSingleTelemetryAsync(singleMessage, deviceConfigurationDto, IngestMessageType.FeatureNotification);
                    }

                    break;
                default: // Unknown
                    // Update UI status
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Unknown Telemetry ingest file type encountered!"));

                    break;
            }

            return isSuccess;
        }

        /// <summary>
        /// Method for sending out a single Telemetry/NRT message to the IoT Hub.
        /// </summary>
        /// <param name="telemetryMessage">Single Telemetry/NRT message content</param>
        /// <param name="deviceConfigurationDto">Details of selected device</param>
        /// <param name="messageType">Optional message type to set as IoT Hub message property</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        /// <remarks>
        /// </remarks>
        public async Task<bool> IngestSingleTelemetryAsync(DeviceTelemetryMessageDto telemetryMessage, DeviceConfigurationDto deviceConfigurationDto, IngestMessageType messageType = IngestMessageType.Unknown)
        {
            bool isSuccess;
            try
            {
                // Prepare device communication to IoT Hub
                string messageContent;
                var message = SerializeTelemetryMessage(telemetryMessage, messageType, out messageContent);

                // Get Device Client reference
                var deviceClient = deviceConfigurationDto.DeviceClient ?? await GetDeviceClient(deviceConfigurationDto);
                if (deviceClient == null)
                {
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"No Device Client available for connecting to IoT Hub!"));
                    return false;
                }

                // Reuse provided Device Client and send out message
                await deviceClient.SendEventAsync(message);

                // Send log message to dedicated Event HUb
                var messageTypeName = message.Properties.ContainsKey("messageType") ? message.Properties["messageType"] : "Unknown";
                string logMessage = $"Single Telemetry Message ({messageTypeName}) for device {deviceConfigurationDto.DeviceId} sent to IoT Hub. [{messageContent}]";
                await LoggingHelper.WriteLogEntryToEventHubAsync(_configuration.RequestId, ConfigurationDto.ComponentName, logMessage, LogEntryType.Info, _configuration.LoggingEventHubConnStr);

                // Update MainWindow with status
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Single Telemetry ({messageTypeName}): <<{messageContent}>> sucessfully sent to IoT Hub!"));
                isSuccess = true;
            }
            catch (Exception ex)
            {
                // Update MainWindow with status
                var exMessage = ex.Message + (ex.InnerException != null ? $"[{ex.InnerException.Message}]" : Empty);
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Single Telemetry NRT/Alert failed sending to IoT Hub with exception: [{exMessage}]!"));
                isSuccess = false;
            }

            return isSuccess;
        }

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
        public async Task<bool> IngestAlertAsync(int statusIndex, DeviceConfigurationDto deviceConfigurationDto)
        {
            var fileName = _configuration.StatusChangeIngestFile[statusIndex];

#if !WINDOWS_UWP
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleFiles", "Alerts", fileName);
#else
            string path;
            try
            {
                var appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var subFolder = await appFolder.GetFolderAsync($"Assets\\SampleFiles\\Alerts");
                var file = await subFolder.GetFileAsync(fileName);
                path = file.Path;
            }
            catch (Exception)
            {
                return false;
            }
#endif

            var sampleFileDto = new TelemetrySampleFileDto()
            {
                FileName = fileName,
                FullPath = path,
                IsZipped = false,
                MessageType = IngestMessageType.Alert
            };

            // Check file existence
            if (!File.Exists(sampleFileDto.FullPath))
            {
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Missing Alert File!"));
                return false;
            }

            return await IngestTelemetryAsync(sampleFileDto, deviceConfigurationDto);
        }

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
        public async Task<bool> IngestTelemetryBatchAsync(List<Message> telemetryMessageBatch, DeviceConfigurationDto deviceConfigurationDto)
        {
            bool isSuccess;
            try
            {
                // Get Device Client reference
                var deviceClient = deviceConfigurationDto.DeviceClient ?? await GetDeviceClient(deviceConfigurationDto);
                if (deviceClient == null)
                {
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"No Device Client available for connecting to IoT Hub!"));
                    return false;
                }

                // Reuse provided Device Client and send out message
                await deviceClient.SendEventBatchAsync(telemetryMessageBatch);

                // Send log message to dedicated Event HUb
                string logMessage = $"Batch (count = {telemetryMessageBatch.Count}) of single Telemetry Messages for device {deviceConfigurationDto.DeviceId} sent to IoT Hub.";
                await LoggingHelper.WriteLogEntryToEventHubAsync(_configuration.RequestId, ConfigurationDto.ComponentName, logMessage, LogEntryType.Info, _configuration.LoggingEventHubConnStr);

                // Update MainWindow with status
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Batch (count = {telemetryMessageBatch.Count}) of single Telemetry messages sucessfully sent to IoT Hub!"));
                isSuccess = true;
            }
            catch (Exception ex)
            {
                // Update MainWindow with status
                var exMessage = ex.Message + (ex.InnerException != null ? $"[{ex.InnerException.Message}]" : Empty);
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Batch (count = {telemetryMessageBatch.Count}) of single Telemetry messages failed sending to IoT Hub with exception: [{exMessage}]!"));
                isSuccess = false;
            }

            return isSuccess;
        }

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
        public async Task<bool> IngestPackageTelemetryAsync(List<DeviceTelemetryMessageDto> messageList, DeviceConfigurationDto deviceConfigurationDto)
        {
            // Create cloud filename from Device Id, current ticks and extension .gz
            string cloudFileName = Utilities.GetGZipedTelemetryPackageFilename(deviceConfigurationDto.DeviceId);

            bool isSuccess;
            try
            {
                // Update UI status with start of upload handling
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Starting to upload Telemetry Package: <<{cloudFileName}>>"));

                // Preference is on Device Certificate
                if (deviceConfigurationDto.DeviceCertificate != null)
                {
                    // Update UI status with end of upload handling
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Telemetry Package Upload to IoT Hub currently DOES NOT support Certificate Authentication!"));
                    return false;
                }

                // Get Device Client reference
                DeviceClient deviceClient = deviceConfigurationDto.DeviceClient ?? await GetDeviceClient(deviceConfigurationDto);
                if (deviceClient == null)
                {
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"No Device Client available for connecting to IoT Hub!"));
                    return false;
                }

                // Reuse provided Device Client
                await UploadTelemetryPackageAsBlob(messageList, deviceConfigurationDto, deviceClient, cloudFileName);

                // Update UI status with end of upload handling
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Telemetry Package sucessfully uploaded to IoT Hub: <<{cloudFileName}>>"));
                isSuccess = true;
            }
            catch (Exception ex)
            {
                // Update UI status with end of upload handling
                string exMessage = ex.Message + (ex.InnerException != null ? $"[{ex.InnerException.Message}]" : Empty);
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Telemetry Package <<{cloudFileName}>> failed uploading to IoT Hub with exception: [{exMessage}]!"));
                isSuccess = false;
            }

            return isSuccess;
        }
#endif

        /// <summary>
        /// Method for handling the Device Monitoring functionality, triggered by a dispatch timer.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device configuration, for which the monitoring has to be handled</param>
        /// <returns><see langword="true"/> if successfully sent out Monitoring Telemetry, otherwise <see langword="false"/></returns>
        /// <remarks>
        /// If the Devive Monitoring dispatch timer is triggerd, a single Telemetry message will be sent out for the Device to the IoT Hub.
        /// </remarks>
        public async Task<bool> SendMonitoringTelemetryAsync(DeviceConfigurationDto deviceConfigurationDto)
        {
            // Preapre Telemetry sample file to use: take the template for single message
            var fileName = "SingleTelemetry.txt";

#if !WINDOWS_UWP
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleFiles", fileName);
#else
            var appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var subFolder = await appFolder.GetFolderAsync($"Assets\\SampleFiles");
            var file = await subFolder.GetFileAsync(fileName);
            var path = file.Path;
#endif

            var sampleFileDto = new TelemetrySampleFileDto()
            {
                FileName = fileName,
                FullPath = path,
                IsZipped = false,
                MessageType = IngestMessageType.NrtTelemetry
            };

            // Check file existence
            if (!File.Exists(sampleFileDto.FullPath))
            {
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Missing NRT File for Monitoring usage!"));
                return false;
            }

            // Now after all preparartions, finally send out some Telemetry message
            var isSuccess = await IngestTelemetryAsync(sampleFileDto, deviceConfigurationDto);

            return isSuccess;
        }

#endregion

        #region Device Feature usage

        /// <summary>
        /// Method for processing a feature en/disable request triggered by specific C2D Command.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device configuration DTO</param>
        /// <param name="deviceCommand">Original C2D Command Message</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
        public async Task<bool> ProcessFeatureEnDisable(DeviceConfigurationDto deviceConfigurationDto, DeviceCommandMessageDto deviceCommand)
        {
            bool isSuccess;

            // Device Twins are only supporetd with the MQTT protocol
            if (Configuration.IotHubTransportType == TransportType.Mqtt ||
                Configuration.IotHubTransportType == TransportType.Mqtt_Tcp_Only ||
                Configuration.IotHubTransportType == TransportType.Mqtt_WebSocket_Only)
            {
                // Get Device Client reference
                var deviceClient = deviceConfigurationDto.DeviceClient ?? await GetDeviceClient(deviceConfigurationDto);
                try
                {
                    // Get Device Twin
                    var deviceTwin = await deviceClient.GetTwinAsync();

                    if (deviceTwin != null)
                    {
                        // Validate current Device Twin and update feature en/disable based on C2D Command
                        isSuccess = await ValidateDeviceTwin(deviceConfigurationDto, deviceCommand, deviceClient, deviceTwin);

                        if (isSuccess)
                            isSuccess = await IngestFeatureAcknowledgetAsync(0, deviceConfigurationDto, deviceCommand);
                    }
                    else
                    {
                        // Update status in UI
                        StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"No Device Twin available for device {deviceConfigurationDto.DeviceId} to en/disable feature!"));
                        isSuccess = false;
                    }
                }
                catch (Exception ex)
                {
                    // Something enexpected and bad happened...
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Processing feature en/disabling (from Twin) failed for device {deviceConfigurationDto.DeviceId} with exception: {ex.Message}!"));
                    isSuccess = false;
                }

                // Update status in UI
                if (isSuccess)
                {
                    var statusMsg = deviceCommand.Feature.IsEnabled ? "enabled" : "disabled";
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Feature {deviceCommand.Feature.FeatureName} successfully {statusMsg} for device {deviceConfigurationDto.DeviceId}!"));
                }
                else
                {
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Processing feature en/disabling (from Twin) failed for device {deviceConfigurationDto.DeviceId} due to acknowledge failure!"));
                }
            }
            else
            {
                // Update status in UI
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Currently configured protocol {Configuration.IotHubTransportType} does NOT support Device Twins!"));
                isSuccess = false;
            }

            return isSuccess;
        }

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
        public async Task<bool> ProcessFeatureUsagetAsync(int statusIndex, DeviceConfigurationDto deviceConfigurationDto, DeviceFeatureDto deviceFeature)
        {
            var fileName = _configuration.StatusChangeIngestFile[statusIndex];
#if !WINDOWS_UWP
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleFiles", "Alerts", fileName);
#else
            var appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var subFolder = await appFolder.GetFolderAsync($"Assets\\SampleFiles\\Alerts");
            var file = await subFolder.GetFileAsync(fileName);
            var path = file.Path;
#endif

            var sampleFileDto = new TelemetrySampleFileDto()
            {
                FileName = fileName,
                FullPath = path,
                IsZipped = false,
                MessageType = IngestMessageType.FeatureNotification
            };

            // Check file existence
            if (!File.Exists(sampleFileDto.FullPath))
            {
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Missing Notification File for Feature Usage!"));
                return false;
            }

            // Prepare specifc command to be used for feature usage notification
            deviceFeature.UsageStatus = FeatureUsageStatus.FeatureExecuted;
            var deviceCommand = new DeviceCommandMessageDto
            {
                EquipmentId = 0,
                CommandType = C2DCommandType.FeatureUsage.ToString(),
                CommandDescription = $"Used feature {deviceFeature.FeatureName}",
                Feature = deviceFeature
            };

            // Validate current Device Twin and update feature en/disable based on C2D Command
            var isSuccess = await UpdateDeviceTwinWithFeatureUsage(deviceConfigurationDto, deviceCommand);

            if (isSuccess)
                isSuccess = await IngestTelemetryAsync(sampleFileDto, deviceConfigurationDto, deviceCommand);

            return isSuccess;
        }

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
        public async Task<bool> IngestFeatureAcknowledgetAsync(int statusIndex, DeviceConfigurationDto deviceConfigurationDto, DeviceCommandMessageDto deviceCommand)
        {
            var fileName = _configuration.StatusChangeIngestFile[statusIndex];
#if !WINDOWS_UWP
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleFiles", "Alerts", fileName);
#else
            var appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var subFolder = await appFolder.GetFolderAsync($"Assets\\SampleFiles\\Alerts");
            var file = await subFolder.GetFileAsync(fileName);
            var path = file.Path;
#endif

            var sampleFileDto = new TelemetrySampleFileDto()
            {
                FileName = fileName,
                FullPath = path,
                IsZipped = false,
                MessageType = IngestMessageType.FeatureNotification
            };

            // Check file existence
            if (!File.Exists(sampleFileDto.FullPath))
            {
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Missing Notification File for Feature Acknowledge!"));
                return false;
            }

            return await IngestTelemetryAsync(sampleFileDto, deviceConfigurationDto, deviceCommand);
        }

        #endregion

        #region C2D Command handling

        /// <summary>
        /// Method for checking for C2D Commands on the IoT HUb (device queue).
        /// </summary>
        /// <param name="deviceConfigurationDto">Config details of selected device</param>
        /// <returns>C2D Command Details</returns>
        /// <remarks>
        /// Method tries to read from the IoT Hub device queue and in case there is a C2D Message, fires an event.
        /// </remarks>
        public async Task<DeviceCommandMessageDto> CheckForCommandAsync(DeviceConfigurationDto deviceConfigurationDto)
        {
            // Prepare device communication to IoT Hub
            DeviceCommandMessageDto deviceCommandMessage = null;

            // Get Device Client reference
            var deviceClient = deviceConfigurationDto.DeviceClient ?? await GetDeviceClient(deviceConfigurationDto);

            // 1st, do we have anything else to do? - Lets check, whether we have to send Telemetry messages, beccause a preveious command enabled that
            if (deviceConfigurationDto.MonitoringDispatcher != null)
            {
                // Yep, we have to send out some Telemetry message - reuse current Device Client
                var isSuccess = await SendMonitoringTelemetryAsync(deviceConfigurationDto);

                if (!isSuccess)
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Monitoring Telemetry Message could not be sent for device {deviceConfigurationDto.DeviceId}!"));
            }

            // Check for Device Message on IoT Hub: wait only up to 5secs
            Message receivedMessage;
            try
            {
                if (Configuration.IotHubTransportType != TransportType.Http1)
                    receivedMessage = await deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(deviceConfigurationDto.DeviceClientReceiveTimeout));
                else
                    receivedMessage = await deviceClient.ReceiveAsync();
            }
            catch (Exception ex)
            {
                // Update UI status with receive timeout info
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"No Message received for device {deviceConfigurationDto.DeviceId} in {deviceConfigurationDto.DeviceClientReceiveTimeout / 1000} secs - exception occured [{ex.Message}]!"));
                return new DeviceCommandMessageDto();
            }

            if (receivedMessage != null)
            {
                // Something received from device queue
                var rawMessage = Empty;
                try
                {
                    // Unpack device message and confirm deletion from device queue
                    rawMessage = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    deviceCommandMessage = JsonConvert.DeserializeObject<DeviceCommandMessageDto>(rawMessage);
                        
                    // Complete on received message is NOT supported with Mqtt
                    await deviceClient.CompleteAsync(receivedMessage);
                }
                catch (JsonException)
                {
                    // Problem occured: put received message back to device queue and return emty DTO
                    // Reject on received message is NOT supported with Mqtt 
                    if (Configuration.IotHubTransportType != TransportType.Mqtt)
                        await deviceClient.RejectAsync(receivedMessage);

                    // Update UI status with end of upload handling
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Message received for device {deviceConfigurationDto.DeviceId} does not contain supported content/command! [{rawMessage}]"));

                    return new DeviceCommandMessageDto();
                }
                catch (Exception ex)
                {
                    // Problem occured: put received message back to device queue and return emty DTO
                    if (Configuration.IotHubTransportType != TransportType.Mqtt)
                        await deviceClient.AbandonAsync(receivedMessage);

                    // Update UI status with end of upload handling
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Message receiving failed for device {deviceConfigurationDto.DeviceId}! [{ex.Message}]"));

                    return new DeviceCommandMessageDto();
                } // try/catch
            } // if message != null

            // Raise Event in case we have a valid command message
            if (deviceCommandMessage != null)
            {
                var invoke = C2DCommandArrived?.Invoke(this, new C2DCommandEventArgs(deviceConfigurationDto, deviceCommandMessage));
                if (invoke != null)
                    await invoke;

                return deviceCommandMessage;
            }

            return new DeviceCommandMessageDto();
        }

        #endregion

        #region Device Twin functionallity

        /// <summary>
        /// Method for retrieving the device features based on the Twin data.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device configuration details</param>
        /// <returns>List of device features</returns>
        /// <remarks>
        /// The internal feature tracking within the Device config details will also be updated.
        /// </remarks>
        public async Task<List<DeviceFeatureDto>> GetDeviceFeaturesFromTwin(DeviceConfigurationDto deviceConfigurationDto)
        {
            // Init
            deviceConfigurationDto.FeatureTracking = new List<DeviceFeatureDto>();

            // Device Twins are only supporetd with the MQTT protocol
            if (Configuration.IotHubTransportType == TransportType.Mqtt ||
                Configuration.IotHubTransportType == TransportType.Mqtt_Tcp_Only ||
                Configuration.IotHubTransportType == TransportType.Mqtt_WebSocket_Only)
            {
                // Get Device Client reference
                var deviceClient = deviceConfigurationDto.DeviceClient ?? await GetDeviceClient(deviceConfigurationDto);
                try
                {
                    // Get Device Twin
                    var deviceTwin = await deviceClient.GetTwinAsync();

                    if (deviceTwin != null)
                    {
                        var curReportedTwinProperties = deviceTwin.Properties.Reported;
                        //List<string> propsToRemove = new List<string>();

                        foreach (KeyValuePair<string,object> twinProperty in curReportedTwinProperties)
                        {
                            // Check if we have a prop value
                            var propValue = JToken.FromObject(twinProperty.Value) as JObject;
                            if (propValue != null && propValue.HasValues)
                            {
                                var reportedFeatureDto = (DeviceFeatureDto) propValue.ToObject(typeof(DeviceFeatureDto));
                                deviceConfigurationDto.FeatureTracking.Add(reportedFeatureDto);
                            }
                            //else
                            //{
                            //    propsToRemove.Add(twinProperty.Key);
                            //    DeviceFeatureDto reportedFeatureDto = new DeviceFeatureDto();
                            //    reportedFeatureDto.FeatureId = -1;
                            //    reportedFeatureDto.FeatureName = propKey;
                            //    reportedFeatureDto.ConfigId = "4711";
                            //    reportedFeatureDto.IsEnabled = false;
                            //    reportedFeatureDto.LastModified = DateTime.UtcNow;
                            //    reportedFeatureDto.Status = "Broken";
                            //    featureList.Add(reportedFeatureDto);
                            //}
                        }

                        //await CleanUpDeviceTwin(deviceClient, propsToRemove);
                    }
                    else
                    {
                        // Update status in UI
                        StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"No Device Twin available for device {deviceConfigurationDto.DeviceId}!"));
                    }
                }
                catch (Exception ex)
                {
                    // Something enexpected and bad happened...
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Retrieving en/disabled features (from Twin) failed for device {deviceConfigurationDto.DeviceId} with exception: {ex.Message}!"));
                }
            }

            // Return list sorted by Feature Id
            deviceConfigurationDto.FeatureTracking.Sort(CompareDeviceFeaturesById);
            return deviceConfigurationDto.FeatureTracking;
        }

        #endregion

        #endregion

        #region Private Methods

        #region General functionallity

        /// <summary>
        /// Helper method for tracking GEO locations used for a device and ro prepare a randomly selected one.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device Geo Tracking list</param>
        /// <returns>Randomly delected GEO location for device</returns>
        /// <remarks>
        /// The method tries to get an not yet used GEO location for an internal table of prepared locations.
        /// As a fallback it returns a &quot;hard-coded&quot; one.
        /// </remarks>
#if !WINDOWS_UWP
        private GeoCoordinate GetRandomGeoCoordinate(DeviceConfigurationDto deviceConfigurationDto)
#else
        private Geopoint GetRandomGeoCoordinate(DeviceConfigurationDto deviceConfigurationDto)
#endif
        {
            var randomGenerator = new Random(DateTime.Now.Millisecond);
#if !WINDOWS_UWP
            GeoCoordinate geoCoordinate;
#else
            Geopoint geoCoordinate;
#endif
            for (var i = 0; i < _configuration.SampleGeoCoordinates.Count; i++)
            {
                var sampleGeoIdx = randomGenerator.Next(0, _configuration.SampleGeoCoordinates.Count - 1);
                geoCoordinate = _configuration.SampleGeoCoordinates[sampleGeoIdx];

                // Try to find GEO location, which has not yet been used for device
                if (geoCoordinate == null || deviceConfigurationDto.GeoTracking.Contains(geoCoordinate))
                    continue;

#if !WINDOWS_UWP
                if (geoCoordinate.IsUnknown != true)
                {
#endif
                    // Found one, use it
                    deviceConfigurationDto.GeoTracking.Add(geoCoordinate);
                    return geoCoordinate;
#if !WINDOWS_UWP
                }
#endif
            }

            // Fallback
#if !WINDOWS_UWP
            geoCoordinate = _configuration.DeviceEmulatorPosition?.Location;

            if (geoCoordinate == null || geoCoordinate.IsUnknown)
            {
                // Last fallback, use hard coded values
                geoCoordinate = new GeoCoordinate(47.831850, 13.061360);
            }
#else
            geoCoordinate = _configuration.DeviceEmulatorPosition?.Coordinate.Point;

            if (geoCoordinate == null)
            {
                // Last fallback, use hard coded values
                var basicPoint = new BasicGeoposition { Latitude = 47.831850, Longitude = 13.061360 };
                geoCoordinate = new Geopoint(basicPoint);
            }
#endif

            return geoCoordinate;
        }

        /// <summary>
        /// Helper method for upading the time tracking per device.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device configuration</param>
        /// <remarks>
        /// This method tries to keep track of the timestamps used for a device, so that with additional sample files and calls,
        /// a consecutive timestamp will be used.
        /// </remarks>
        private void UpdateDevieTimeTracking(DeviceConfigurationDto deviceConfigurationDto)
        {
            if (IsNullOrEmpty(deviceConfigurationDto?.DeviceId))
                return;

            if (deviceConfigurationDto.TimeTracking == DateTime.MinValue)
            {
                deviceConfigurationDto.TimeTracking = DateTime.UtcNow.AddMinutes(-15);
            }
            else
            {
                deviceConfigurationDto.TimeTracking = deviceConfigurationDto.TimeTracking.AddMinutes(15);
                var randomGenerator = new Random(DateTime.Now.Millisecond);
                deviceConfigurationDto.TimeTracking = deviceConfigurationDto.TimeTracking.AddMinutes(randomGenerator.Next(1, 5));
            }
        }

        #endregion

        #region Telemetry Data handling

#if !WINDOWS_UWP
        /// <summary>
        /// Helper method for uploading a Blob to IoT Hub.
        /// </summary>
        /// <param name="messageList">List of Telemetry messages to package</param>
        /// <param name="deviceConfigurationDto">Device config details</param>
        /// <param name="deviceClient">Reference to Device Client</param>
        /// <param name="cloudFileName">Name of Telemetry package in Blob Storage</param>
        /// <remarks>
        /// The list of Telemetry message objects will be serialized to JSON, compressed as GZip and finally uploaded to the IoT Hub.
        /// </remarks>
        private async Task UploadTelemetryPackageAsBlob(List<DeviceTelemetryMessageDto> messageList, DeviceConfigurationDto deviceConfigurationDto, DeviceClient deviceClient, string cloudFileName)
        {
            using (MemoryStream compressedJsonMemStream = new MemoryStream())
            {
                using (var rwaJsonMemStream = new MemoryStream())
                {
                    using (StreamWriter streamWriter = new StreamWriter(rwaJsonMemStream))
                    {
                        using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                        {
                            // Init JSON Serializer
                            var serializer = new JsonSerializer();
                            serializer.Converters.Add(new StringEnumConverter());
                            serializer.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                            serializer.NullValueHandling = NullValueHandling.Ignore;
                            serializer.Formatting = Formatting.Indented;

                            // Serialize JSON Objects
                            serializer.Serialize(jsonTextWriter, messageList.ToArray());
                            jsonTextWriter.Flush();
                            rwaJsonMemStream.Seek(0, SeekOrigin.Begin);

                            // Now its time GZip the JSON data
                            using (GZipStream compressionStream = new GZipStream(compressedJsonMemStream, CompressionMode.Compress))
                            {
                                rwaJsonMemStream.CopyTo(compressionStream);
                            }

                            // Just to make sure that we have the full compress JSON, lets buffer it for a moment...
                            var buffer = compressedJsonMemStream.ToArray();
                            using (MemoryStream bufferedJsonMemStream = new MemoryStream(buffer))
                            {
                                // Finally, upload GZip to IoT Hub
                                await deviceClient.UploadToBlobAsync(cloudFileName, bufferedJsonMemStream);

                                // Send log message to dedicated Event HUb
                                string logMessage = $"Telemetry Package for device {deviceConfigurationDto.DeviceId} uploaded to IoT Hub: {cloudFileName}";
                                await LoggingHelper.WriteLogEntryToEventHubAsync(_configuration.RequestId, ConfigurationDto.ComponentName, logMessage, LogEntryType.Info, _configuration.LoggingEventHubConnStr);
                            } // memoryStream
                        } // jsonTextWriter
                    } // streamWriter
                } // rwaJsonMemStream
            } // compressedJsonMemStream
        }
#endif

        /// <summary>
        /// Helper method for reading JSON Telemetry messages from a selected sample text file.
        /// </summary>
        /// <param name="telemetryIngestFile">Details of selected sample Telemetry text file</param>
        /// <returns>List of JSON objects</returns>
        /// <remarks>
        /// </remarks>
        private async Task<List<DeviceTelemetryMessageDto>> ReadTelemetryMessagesFromTextAsync(TelemetrySampleFileDto telemetryIngestFile)
        {
#if WINDOWS_UWP
            var file = await StorageFile.GetFileFromPathAsync(telemetryIngestFile.FullPath);
#endif

#if !WINDOWS_UWP
            // Not GZiped Telemetry data, either single one or batch
            var messageArray = await Task.Run(() =>
            {
                using (StreamReader streamReader = new StreamReader(telemetryIngestFile.FullPath))
                {
                    using (JsonReader jsonTextReader = new JsonTextReader(streamReader))
                    {
                        // Read all JSON Objects from stream
                        var serializer = new JsonSerializer();
                        serializer.Converters.Add(new StringEnumConverter());
                        serializer.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                        serializer.NullValueHandling = NullValueHandling.Ignore;
                        return serializer.Deserialize<DeviceTelemetryMessageDto[]>(jsonTextReader).ToList();
                    }
                }
            });
            return messageArray.ToList();
#else
            var messageArray = new List<DeviceTelemetryMessageDto>();

            //using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
            //{
            //    using (DataReader dataReader = new DataReader(readStream))
            //    {
            //        ulong size = readStream.Size;
            //        if (size <= uint.MaxValue)
            //        {
            //            DeviceTelemetryMessageDto[] messageDtoArray;
            //            try
            //            {
            //                uint numBytesLoaded = await dataReader.LoadAsync((uint)size);
            //                string fileContent = dataReader.ReadString(numBytesLoaded);
            //                JsonSerializerSettings jsonSettings = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None};
            //                messageDtoArray = JsonConvert.DeserializeObject<DeviceTelemetryMessageDto[]>(fileContent, jsonSettings);
            //            }
            //            catch (Exception)
            //            {
            //                messageDtoArray = new DeviceTelemetryMessageDto[0];
            //            }
            //            messageArray = messageDtoArray.ToList();
            //        }
            //    }
            //}
            var jsonText = await FileIO.ReadTextAsync(file);
            try
            {
                var jsonSerializer = new DataContractJsonSerializer(typeof(DeviceTelemetryMessageDto));
                var jsonArray = JsonArray.Parse(jsonText);
                foreach (var arrayValue in jsonArray)
                {
                    var jsonValue = (JsonValue)arrayValue;
                    var jsonObject = jsonValue.GetObject();
                    using (var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonObject.ToString())))
                    {
                        var messageDto = (DeviceTelemetryMessageDto)jsonSerializer.ReadObject(jsonStream);
                        messageArray.Add(messageDto);
                    }
                }
            }
            catch (Exception)
            {
                // Nothing to do here
            }
            return messageArray;
#endif
        }

#if !WINDOWS_UWP
        /// <summary>
        /// Helper method for reading JSON Telemetry messages from a selected sample GZip file
        /// </summary>
        /// <param name="telemetryIngestFile">Details of selected sample Telemetry text file</param>
        /// <returns>List of JSON objects read</returns>
        /// <remarks></remarks>
        private async Task<List<DeviceTelemetryMessageDto>> ReadTelemetryMessagesFromGZipAsync(TelemetrySampleFileDto telemetryIngestFile)
        {
            var messageList = await Task.Run(() =>
            {
                List<DeviceTelemetryMessageDto> list = new List<DeviceTelemetryMessageDto>();
                using (FileStream originalFileStream = new FileStream(telemetryIngestFile.FullPath, FileMode.Open, FileAccess.Read))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        using (StreamReader streamReader = new StreamReader(decompressionStream))
                        {
                            using (JsonReader jsonTextReader = new JsonTextReader(streamReader))
                            {
                                while (jsonTextReader.Read())
                                {
                                    // Expecting beginning of an Array
                                    if (jsonTextReader.TokenType != JsonToken.StartArray)
                                    {
                                        continue;
                                    }

                                    while (jsonTextReader.Read())
                                    {
                                        // Lets get next full JSON Object
                                        if (jsonTextReader.TokenType != JsonToken.StartObject)
                                        {
                                            continue;
                                        }

                                        // Read JSON Object by Object, deserialize and update it with current Device Id and timestmap
                                        JObject obj = JObject.Load(jsonTextReader);
                                        JsonSerializerSettings jsonSettings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore };
                                        var singleMessage = JsonConvert.DeserializeObject<DeviceTelemetryMessageDto>(obj.ToString(), jsonSettings);
                                        list.Add(singleMessage);
                                    }
                                }
                            } // JSON TextReader
                        } // StreamReader
                    } // GZipStream
                } // FileStream

                return list;
            });

            return messageList;
        }
#endif

        /// <summary>
        /// Helper method for updating the data of a single telemetry/NRT message.
        /// </summary>
        /// <param name="telemetryMessage">Object representation of message</param>
        /// <param name="deviceConfigurationDto">Device configuration details</param>
        /// <param name="geoCoordinate">Initial GEO coordinate</param>
        /// <param name="isFirstMessage"></param>
        /// <returns>Updated Telemetry message</returns>
        /// <remarks>
        /// Device Id and UNIX Timestamp will be alsways updated, consuption and usage data only, if some value is exisitng (&gt; 0).
        /// Geo location will also only be updated, if there is some old value.
        /// This behavior is because of the delta messages, that might be contained in a Telemetry package.
        /// </remarks>
#if !WINDOWS_UWP
        private void UpdateTelemetryMessage(DeviceTelemetryMessageDto telemetryMessage, DeviceConfigurationDto deviceConfigurationDto, ref GeoCoordinate geoCoordinate, bool isFirstMessage = false)
#else
        private void UpdateTelemetryMessage(DeviceTelemetryMessageDto telemetryMessage, DeviceConfigurationDto deviceConfigurationDto, ref Geopoint geoCoordinate, bool isFirstMessage = false)
#endif
        {
            // Mandatory cehcking
            if (telemetryMessage == null)
                return;

            // Update device Id
            long convertedId;
            var isSuccess = long.TryParse(deviceConfigurationDto.DeviceId, NumberStyles.Integer, CultureInfo.InvariantCulture, out convertedId);
            telemetryMessage.EquipmentId = isSuccess ? convertedId : 123456789012;

            // Update timestamp, using UNIX timestamp (epoch)
            var dateTimeOffset = new DateTimeOffset(deviceConfigurationDto.TimeTracking);
            telemetryMessage.TimeStamp = dateTimeOffset.ToUnixTimeMilliseconds();

            // Update some device values - but only if there was a value previously
            var randomGenerator = new Random(DateTime.Now.Millisecond);
            if (telemetryMessage.Values?.ConsumptionCrane != null)
                telemetryMessage.Values.ConsumptionCrane = randomGenerator.Next(15, 26);
            if (telemetryMessage.Values?.ConsumptionTruck != null)
                telemetryMessage.Values.ConsumptionTruck = randomGenerator.Next(30, 46);
            if (telemetryMessage.Values?.OperatingHoursCrane != null)
                telemetryMessage.Values.OperatingHoursCrane = randomGenerator.Next(0, 6);

            // Update geo coordinates with current coordinates of machine running this application
            if (telemetryMessage.Values?.Latitude != null || telemetryMessage.Values?.Longitude != null)
            {
                if (isFirstMessage)
                    geoCoordinate = GetRandomGeoCoordinate(deviceConfigurationDto);

                // Update values, but only if we have some GEO location
                if (geoCoordinate != null)
                {
#if !WINDOWS_UWP
                    telemetryMessage.Values.Longitude = geoCoordinate.Longitude;
                    telemetryMessage.Values.Latitude = geoCoordinate.Latitude;
#else
                    telemetryMessage.Values.Longitude = geoCoordinate.Position.Longitude;
                    telemetryMessage.Values.Latitude = geoCoordinate.Position.Latitude;
#endif
                }
            }

            // Lets have a time difference of 36sec berween each message (makes 5 messages per 3min, 25 messages per 15min)
            deviceConfigurationDto.TimeTracking = deviceConfigurationDto.TimeTracking.AddSeconds(36.0);
        }

        /// <summary>
        /// Helper method for serializing a given Telemetry message to JSON and preparing as IoT Hub message.
        /// </summary>
        /// <param name="telemetryMessage">Telemetry NRT/ALert message</param>
        /// <param name="messageType">Optional message type to set on IoT Hub message</param>
        /// <returns>IoT Hub message</returns>
        /// <remarks>
        /// This method adds a message type proeprty to the IoT Hub message sent out.
        /// If the <see cref="Values.CurrentErrors"/> property of the <param ref="telemetryMessage"></param> is not empty, 
        /// the message is considered to be an Alert, otherwise it is considered to be a single NRT message.
        /// </remarks>
        /// <overloads>There is one overload for this heloper method</overloads>
        private static Message SerializeTelemetryMessage(DeviceTelemetryMessageDto telemetryMessage, IngestMessageType messageType = IngestMessageType.Unknown)
        {
            string messageContent;
            return SerializeTelemetryMessage(telemetryMessage, messageType, out messageContent);
        }

        /// <summary>
        /// Helper method for serializing a given Telemetry message to JSON and preparing as IoT Hub message.
        /// </summary>
        /// <param name="telemetryMessage">Telemetry NRT/ALert message</param>
        /// <param name="messageType">Message type to set on IoT Hub message</param>
        /// <param name="messageContent">[out] Telemetry message as JSON</param>
        /// <returns>IoT Hub message</returns>
        /// <remarks>
        /// This method adds a message type proeprty to the IoT Hub message sent out.
        /// If the <see cref="Values.CurrentErrors"/> property of the <param ref="telemetryMessage"></param> is not empty, 
        /// the message is considered to be an Alert, otherwise it is considered to be a single NRT message.
        /// </remarks>
        /// <overloads>There is one overload for this heloper method</overloads>
        private static Message SerializeTelemetryMessage(DeviceTelemetryMessageDto telemetryMessage, IngestMessageType messageType, out string messageContent)
        {
            messageContent = JsonConvert.SerializeObject(telemetryMessage);
            var message = new Message(Encoding.ASCII.GetBytes(messageContent));

            if (telemetryMessage.Values?.CurrentErrors != null && telemetryMessage.Values.CurrentErrors.Length > 0)
                messageType = IngestMessageType.Alert;
            else if (messageType == IngestMessageType.Unknown)
                messageType = IngestMessageType.NrtTelemetry;
            message.Properties["messageType"] = messageType.ToString();

            return message;
        }

        #endregion

        #region Device Feature usage

        /// <summary>
        /// Helper method for updating the data of a single telemetry/NRT message.
        /// </summary>
        /// <param name="telemetryMessage">Object representation of message</param>
        /// <param name="deviceConfigurationDto">Device configuration details</param>
        /// <param name="geoCoordinate">Initial GEO coordinate</param>
        /// <param name="deviceCommand">C2D Command details, which should contain details about a Feature Notification</param>
        /// <param name="isFirstMessage"></param>
        /// <returns>Updated Telemetry message</returns>
        /// <remarks>
        /// Device Id and UNIX Timestamp will always be updated. In addition the Feature property will be set.
        /// Geo location will also only be updated, if there is some old value.
        /// </remarks>
#if !WINDOWS_UWP
        private void UpdateFeatureNotificationMessage(DeviceTelemetryMessageDto telemetryMessage, DeviceConfigurationDto deviceConfigurationDto, ref GeoCoordinate geoCoordinate, DeviceCommandMessageDto deviceCommand, bool isFirstMessage = false)
#else
        private void UpdateFeatureNotificationMessage(DeviceTelemetryMessageDto telemetryMessage, DeviceConfigurationDto deviceConfigurationDto, ref Geopoint geoCoordinate, DeviceCommandMessageDto deviceCommand, bool isFirstMessage = false)
#endif
        {
            // Update device Id
            long convertedId;
            var isSuccess = long.TryParse(deviceConfigurationDto.DeviceId, NumberStyles.Integer, CultureInfo.InvariantCulture, out convertedId);
            telemetryMessage.EquipmentId = isSuccess ? convertedId : 123456789012;

            // Update timestamp, using UNIX timestamp (epoch)
            var dateTimeOffset = new DateTimeOffset(deviceConfigurationDto.TimeTracking);
            telemetryMessage.TimeStamp = dateTimeOffset.ToUnixTimeMilliseconds();

            // Set Feature details
            if (deviceCommand?.Feature != null)
                telemetryMessage.Feature = deviceCommand.Feature;

            // Update geo coordinates with current coordinates of machine running this application
            if (telemetryMessage.Values.Latitude.HasValue || telemetryMessage.Values.Longitude.HasValue)
            {
                if (isFirstMessage)
                    geoCoordinate = GetRandomGeoCoordinate(deviceConfigurationDto);

                // Update values, but only if we have some GEO location
                if (geoCoordinate != null)
                {
#if !WINDOWS_UWP
                    telemetryMessage.Values.Longitude = geoCoordinate.Longitude;
                    telemetryMessage.Values.Latitude = geoCoordinate.Latitude;
#else
                    telemetryMessage.Values.Longitude = geoCoordinate.Position.Longitude;
                    telemetryMessage.Values.Latitude = geoCoordinate.Position.Latitude;
#endif
                }
            }

            // Lets have a time difference of 36sec berween each message (makes 5 messages per 3min, 25 messages per 15min)
            deviceConfigurationDto.TimeTracking = deviceConfigurationDto.TimeTracking.AddSeconds(36.0);
        }

        /// <summary>
        /// Helper method for comparing two Device Features by their Id.
        /// </summary>
        /// <param name="feature1">Device Feature 1</param>
        /// <param name="feature2">Device Feature 2</param>
        /// <returns>0, if both are equal (or <see langword="null"/>), 1 if 1st one is greater than 2nd or 2nd is <see langword="null"/>, -1 if 1st one is less than 2nd or 1st is <see langword="null"/></returns>
        private static int CompareDeviceFeaturesById(DeviceFeatureDto feature1, DeviceFeatureDto feature2)
        {
            if (feature1 == null && feature2 == null)
                return 0;

            if (feature1 == null)
                return -1;

            if (feature2 == null)
                return 1;

            if (feature1.FeatureId < feature2.FeatureId)
                return -1;

            return feature1.FeatureId > feature2.FeatureId ? 1 : 0;
        }

        #endregion

        #region Device Twin functionallity

        /// <summary>
        /// Helper method for updating the Feature Credit Count within the Device Twin.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device configuration details, also containing the tracked Value Sums</param>
        /// <param name="deviceCommand">Original C2D Command to get Feature details from</param>
        /// <returns><see langword="true"/> if successfully updated the Device Twin, <see langword="false"/> otherwise</returns>
        private async Task<bool> UpdateDeviceTwinWithFeatureUsage(DeviceConfigurationDto deviceConfigurationDto, DeviceCommandMessageDto deviceCommand)
        {
            // Init
            var isSuccessfullyUpdated = false;

            // Device Twins are only supporetd with the MQTT protocol
            if (Configuration.IotHubTransportType == TransportType.Mqtt ||
                Configuration.IotHubTransportType == TransportType.Mqtt_Tcp_Only ||
                Configuration.IotHubTransportType == TransportType.Mqtt_WebSocket_Only)
            {
                // Get Device Client reference
                var deviceClient = deviceConfigurationDto.DeviceClient ?? await GetDeviceClient(deviceConfigurationDto);
                try
                {
                    // Get Device Twin
                    var deviceTwin = await deviceClient.GetTwinAsync();

                    if (deviceTwin != null)
                    {
                        var featurePropName = deviceCommand.Feature.FeatureName.Replace(" ", "_");
                        var curReportedTwinProperties = deviceTwin.Properties.Reported;
                        var updReportedTwinProperties = new TwinCollection();

                        // Now check, if it was already reported before and has new config Id now
                        JObject featureJson;
                        DeviceFeatureDto updReportedFeatureDto;
                        if (curReportedTwinProperties.Contains(featurePropName))
                        {
                            JObject reportedFeatureProp = deviceTwin.Properties.Reported[featurePropName];
                            updReportedFeatureDto = (DeviceFeatureDto) reportedFeatureProp.ToObject(typeof(DeviceFeatureDto));

                            // Updated exisitng feature config due to differing config Id
                            updReportedFeatureDto.UsageStatus = FeatureUsageStatus.FeatureExecuted;
                            updReportedFeatureDto.CreditCount--;
                            updReportedFeatureDto.LastModified = DateTime.UtcNow;
                            featureJson = (JObject) JToken.FromObject(updReportedFeatureDto);
                        }
                        else
                        {
                            // Not reported yet, so lets add it as new one
                            updReportedFeatureDto = new DeviceFeatureDto
                            {
                                UsageStatus = FeatureUsageStatus.FeatureExecuted
                            };
                            updReportedFeatureDto.CreditCount--;
                            updReportedFeatureDto.LastModified = DateTime.UtcNow;
                            featureJson = (JObject) JToken.FromObject(updReportedFeatureDto);
                        }

                        // Update reported properties on Device Twin
                        updReportedTwinProperties[featurePropName] = featureJson;
                        await deviceClient.UpdateReportedPropertiesAsync(updReportedTwinProperties);

                        // Update internal tracking
                        var selReportedFeatureDto = deviceConfigurationDto.FeatureTracking.Find(feature => feature.FeatureName == featurePropName);
                        selReportedFeatureDto.CreditCount = updReportedFeatureDto.CreditCount;
                        selReportedFeatureDto.LastModified = updReportedFeatureDto.LastModified;

                        isSuccessfullyUpdated = true;
                    } // if deviceTwin != null
                }
                catch (Exception ex)
                {
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Updating reported Feature Credit Count property failed for Device Twin {deviceConfigurationDto.DeviceId} with exception: {ex.Message}!"));
                    isSuccessfullyUpdated = false;
                } // try/catch
            } // if MQTT

            return isSuccessfullyUpdated;
        }

        /// <summary>
        /// Helper method for validating the feature en/disable from the C2D Command and updating the Device Twin.
        /// </summary>
        /// <param name="deviceConfigurationDto">Device confguration DTO</param>
        /// <param name="deviceCommand">Original C2D Command</param>
        /// <param name="deviceClient">Reference to device client</param>
        /// <param name="deviceTwin">Current Device Twin</param>
        /// <returns><see langword="true"/> if successfully en/disabled the feature, <see langword="false"/> otherwise</returns>
        private async Task<bool> ValidateDeviceTwin(DeviceConfigurationDto deviceConfigurationDto, DeviceCommandMessageDto deviceCommand, DeviceClient deviceClient, Twin deviceTwin)
        {
            // Init
            var isSuccessfullyValidated = true;

            // Mandatory checking
            if ((deviceClient == null) | (deviceTwin?.Properties?.Desired == null))
                return false;

            // Check, if any desired prop matches the feature from the C2D Command
            var featurePropName = deviceCommand.Feature.FeatureName.Replace(" ", "_");
            if (deviceTwin.Properties.Desired.Contains(featurePropName))
            {
                // Get reported props and prepare new collection for update
                var curReportedTwinProperties = deviceTwin.Properties.Reported;
                var updReportedTwinProperties = new TwinCollection();

                // Now check, if it was already reported before and has new config Id now
                if (curReportedTwinProperties.Contains(featurePropName))
                {
                    JObject reportedFeatureProp = deviceTwin.Properties.Reported[featurePropName];
                    var reportedFeatureDto = (DeviceFeatureDto)reportedFeatureProp.ToObject(typeof(DeviceFeatureDto));

                    if (reportedFeatureDto.ConfigId != deviceCommand.Feature.ConfigId)
                    {
                        // Updated exisitng feature config due to differing config Id
                        deviceCommand.Feature.Status = "Success";
                        deviceCommand.Feature.ValidationCount = 0;
                        deviceCommand.Feature.LastModified = DateTime.UtcNow;
                        var featureJson = (JObject)JToken.FromObject(deviceCommand.Feature);
                        updReportedTwinProperties[featurePropName] = featureJson;
                    }
                    else
                    {
                        // Reported feature and Command feature have same config Id
                        StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Desired property in Device Twin is up-to-date for feature {deviceCommand.Feature.FeatureName} for device {deviceConfigurationDto.DeviceId}!"));
                    }
                }
                else
                {
                    // Not reported yet, so lets add it
                    deviceCommand.Feature.Status = "Success";
                    deviceCommand.Feature.ValidationCount = 0;
                    deviceCommand.Feature.LastModified = DateTime.UtcNow;
                    var featureJson = (JObject)JToken.FromObject(deviceCommand.Feature);
                    updReportedTwinProperties[featurePropName] = featureJson;
                }

                try
                {
                    // Update reported properties on Device Twin
                    await deviceClient.UpdateReportedPropertiesAsync(updReportedTwinProperties);
                }
                catch (Exception ex)
                {
                    StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Updating reported property failed in Device Twin for feature {deviceCommand.Feature.FeatureName} for device {deviceConfigurationDto.DeviceId} with exception: {ex.Message}!"));
                    isSuccessfullyValidated = false;
                }
            }
            else
            {
                // No desired prop matches feature from C2D Command
                StatusUpdateChange?.Invoke(this, new StatusChangeEventArgs($"Desired property not found in Device Twin for feature {deviceCommand.Feature.FeatureName} for device {deviceConfigurationDto.DeviceId}!"));
                isSuccessfullyValidated = false;
            }

            return isSuccessfullyValidated;
        }

        ///// <summary>
        ///// Helper method for removing given list of (reported) properties from the Device Twin.
        ///// </summary>
        ///// <param name="deviceClient">Reference to Device Client</param>
        ///// <param name="propsToRemove">List of reported Device Twin property names to remove</param>
        ///// <returns>Updated Device Twin</returns>
        //private static async Task<Twin>  CleanUpDeviceTwin(DeviceClient deviceClient, IReadOnlyCollection<string> propsToRemove)
        //{
        //    // Prepare update Twin Collection
        //    TwinCollection updReportedTwinProperties = new TwinCollection();

        //    if (propsToRemove != null && propsToRemove.Count > 0)
        //    {
        //        foreach (string propName in propsToRemove)
        //        {
        //            updReportedTwinProperties[propName] = new JObject();

        //        }
        //        updReportedTwinProperties.ClearMetadata();
        //        await deviceClient.UpdateReportedPropertiesAsync(updReportedTwinProperties);
        //    }

        //    // Return updated Device Twin
        //    Twin updatedTwin = await deviceClient.GetTwinAsync();
        //    return updatedTwin;
        //}

        #endregion

        #endregion
    }
}
