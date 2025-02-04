using System;
#if !WINDOWS_UWP
using System.Configuration;
using System.Device.Location;
using System.IO;
#else
using Microsoft.Devices.Tpm;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
// Azure stuff
using Microsoft.Azure.Devices;
using DeviceClient = Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
// Own usings
using Common.Enums;

namespace DeviceEmulator.Dto
{
    /// <summary>
    /// Classs implementing the Device Emulator configuration DTO.
    /// </summary>
    /// <remarks>
    /// The emulator configuration contains the following:
    /// <list type="bullet">
    /// <item><description>IoT Hub connection details</description></item>
    /// <item><description>list of testable devices (cranes)</description></item>
    /// </list>
    /// </remarks>
    public class ConfigurationDto
    {
        #region Member(s)

        /// <summary>
        /// Static value for the Device Emulator's component name.
        /// </summary>
        /// <remarks>
        /// The component name is primarily used for loggong output.
        /// </remarks>
        public static string ComponentName = "DeviceEmulator";

        // Azure IoT Hub device registry management
        private RegistryManager _registryManager;
        // Flag indicating whether to use Device Certificates (if any) or not
        private bool _useDeviceCertificate;

#if WINDOWS_UWP
        // Config container for UWP - instead of app.config
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Property for the pre-configured devices
        /// </summary>
        /// <value>Get or sets the pre-configured devices</value>
        /// <remarks>
        /// All devices supported by the emulator (device pool) are pre-configured.
        /// </remarks>
        public List<DeviceConfigurationDto> DeviceConfiguration { get; set; } = new List<DeviceConfigurationDto>();

        /// <summary>
        /// Property for tracking the status change of ingest files.
        /// </summary>
        /// <value>Get or sets the status change of ingest files</value>
        public Dictionary<int, string> StatusChangeIngestFile { get; set; } = new Dictionary<int, string>();

        /// <summary>
        /// Property for a set of sample GEO locations.
        /// </summary>
        /// <value>Gets or sets the set of sample GEO locations</value>
#if !WINDOWS_UWP
        public Dictionary<int, GeoCoordinate> SampleGeoCoordinates { get; set; } = new Dictionary<int, GeoCoordinate>()
        {
            { 0,  new GeoCoordinate(47.831850, 13.061360)},
            { 1,  new GeoCoordinate(47.835940, 13.055155)},
            { 2,  new GeoCoordinate(47.842815, 13.070154)},
            { 3,  new GeoCoordinate(47.830961, 13.082249)},
            { 4,  new GeoCoordinate(47.818694, 13.106674)},
            { 5,  new GeoCoordinate(47.791123, 13.069020)},
        };
#else
        public Dictionary<int, Geopoint> SampleGeoCoordinates { get; set; } = new Dictionary<int, Geopoint>();

#endif

        /// <summary>
        /// Property for the request Id.
        /// </summary>
        /// <value>Gets or sets request Id</value>
        /// <remarks>
        /// Value is used for consolidating the looging messages of the Device Emulator. It will be created once at start time.
        /// </remarks>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Property for the Transport Type to be used for the IoT Hub.
        /// </summary>
        /// <value>Gets the Transport Type to be used for the IoT Hub</value>
        /// <remarks>
        /// Value is initialized from app.config.
        /// </remarks>
        public DeviceClient.TransportType IotHubTransportType { get; set; }

        /// <summary>
        /// Property for the connection string for IoT Hub device registry management.
        /// </summary>
        /// <value>Gets the connection string for IoT Hub device registry management</value>
        /// <remarks>
        /// Value is initialized from app.config.
        /// </remarks>
        public string IotHubRegistryConnStr { get; set;  }

        /// <summary>
        /// Property for the IoT Hub hostname.
        /// </summary>
        /// <value>Gets the IoT Hub hostname</value>
        /// <remarks>
        /// Value is initialized from app.config.
        /// </remarks>
        public string HostNameIoT { get; set; }

        /// <summary>
        /// Property for the connection string of the logging Event Hub.
        /// </summary>
        /// <value>Gets the connection string of the logging Event Hub</value>
        /// <remarks>
        /// Value is initialized from app.config.
        /// </remarks>
        public string LoggingEventHubConnStr { get; set; }

        /// <summary>
        /// Property for the name of the logging Event Hub.
        /// </summary>
        /// <value>Gets the name of the logging Event Hub</value>
        /// <remarks>
        /// Value is initialized from app.config.
        /// </remarks>
        public string LoggingEventHubName { get; set; }

        /// <summary>
        /// Property for the name of the default Telemetry Sample file to be used for C2D Command.
        /// </summary>
        /// <value>Gets the name of the the default Telemetry Sample file to be used for C2D Command</value>
        /// <remarks>
        /// Value is initialized from app.config.
        /// </remarks>
        public string DefaultRefreshTelemetrySample { get; set; }

        /// <summary>
        /// Property for the current Geo Position.
        /// </summary>
        /// <value>Gets or sets the current Geo Position</value>
        /// <remarks>
        /// Value is initialized only once.
        /// </remarks>
#if !WINDOWS_UWP
        public GeoPosition<GeoCoordinate> DeviceEmulatorPosition { get; set; }
#else
        public Geoposition DeviceEmulatorPosition { get; set; }
#endif

        /// <summary>
        /// Property for the current environment as configured in app.config.
        /// </summary>
        /// <value>Gets or sets the current environment as configured in app.config</value>
        /// <remarks>
        /// This is just for informational purpose to signal the current environment usage, 
        /// as it has been configured in regards of Azure resources.
        /// </remarks>
        public string Environment { get; set; }

        /// <summary>
        /// Property for the lower range value of the receice waiting deplay.
        /// </summary>
        /// <value>Gets or sets the lower range value of the receice waiting deplay</value>
        /// <remarks>
        /// The waiting delay is given in seconds.
        /// </remarks>
        public int ReceiceWaitLower { get; set; }

        /// <summary>
        /// Property for the upper range value of the receice waiting deplay.
        /// </summary>
        /// <value>Gets or sets the upper range value of the receice waiting deplay</value>
        /// <remarks>
        /// The waiting delay is given in seconds.
        /// </remarks>
        public int ReceiceWaitUpper { get; set; }

        /// <summary>
        /// Property for the monitoring timespan (in secs).
        /// </summary>
        /// <value>Gets or sets the monitoring timespan (in secs)</value>
        /// <remarks>
        /// The timespan in seconds used for the monitoring functionality - 
        /// if active, every x secs, a single Telemetry message will be sent by the Device.
        /// </remarks>
        public int MonitoringTimespan { get; set; }

        /// <summary>
        /// Property for the Device Client Operation Timeout (in millisecs).
        /// </summary>
        /// <value>Gets or sets the Device Client Operation Timeout (in millisecs)</value>
        /// <remarks>
        /// </remarks>
        public uint DeviceClientOperationTimeout { get; set; }

        /// <summary>
        /// Property for the Device Client Receice Timeout (in millisecs).
        /// </summary>
        /// <value>Gets or sets the Device Client Receice Timeout (in millisecs)</value>
        /// <remarks>
        /// </remarks>
        public uint DeviceClientReceiveTimeout { get; set; }

        /// <summary>
        /// Property for the switch to receice C2D Commands for single or multiple devices.
        /// </summary>
        /// <value>Gets or sets the switch to receice C2D Commands for single or multiple devices</value>
        /// <remarks>
        /// </remarks>
        public bool ReceiceC2DForAll { get; set; }

        /// <summary>
        /// Property for the Device Type of this emulator.
        /// </summary>
        /// <value>Gets or sets the Device Type of this emulator</value>
        /// <remarks>
        /// This property should refelect the type of the device, the emulator is currently running on, e.g. desktop, tablet, IoT, etc.
        /// </remarks>
        public EmulatorDeviceTypeEnum EmulatorDeviceType { get; set; }

        #endregion

        #region Constructor

#if !WINDOWS_UWP
        /// <summary>
        /// Default constructor (non UWP).
        /// </summary>
        /// <remarks>
        /// Retrieves the IoT Hub relevant connection details from the config file.
        /// </remarks>
        public ConfigurationDto()
        {
            // Try to evaluate desired TransportType for IoT Hub communication
            IotHubTransportType = DeviceClient.TransportType.Mqtt;
            DeviceClient.TransportType transportTypeSendConfigured;
            if (Enum.TryParse(ConfigurationManager.AppSettings["IotHubTransportType"], true, out transportTypeSendConfigured))
            {
                IotHubTransportType = transportTypeSendConfigured;
            }

            // Get IoT Hub connectivity details from app.config
            IotHubRegistryConnStr = ConfigurationManager.AppSettings["IotHubRegistryConnStr"];
            HostNameIoT = ConfigurationManager.AppSettings["HostNameIoT"];

            // For Logging Sink =  Event Hub
            LoggingEventHubName = ConfigurationManager.AppSettings["LoggingEventHubName"];
            LoggingEventHubConnStr = ConfigurationManager.AppSettings["LoggingEventHubConnStr"];

            // Get the name of the default Telemetry Sample file to be used for C2D Command
            DefaultRefreshTelemetrySample = ConfigurationManager.AppSettings["DefaultRefreshTelemetrySample"];

            // Get the current environment
            Environment = ConfigurationManager.AppSettings["Environment"];

            // Get flag value for Device Certificate usage
            bool.TryParse(ConfigurationManager.AppSettings["UseDeviceCertificate"], out _useDeviceCertificate);

            // Get delay (in secs) to use for receiving C2D Commands and other Timeouts
            int configuredSignedValue;
            ReceiceWaitLower = int.TryParse(ConfigurationManager.AppSettings["ReceiceWaitLower"], out configuredSignedValue) ? configuredSignedValue : 10;
            ReceiceWaitUpper = int.TryParse(ConfigurationManager.AppSettings["ReceiceWaitUpper"], out configuredSignedValue) ? configuredSignedValue : 20;
            MonitoringTimespan = int.TryParse(ConfigurationManager.AppSettings["MonitoringTimespan"], out configuredSignedValue) ? configuredSignedValue : 5;

            uint configuredUnsignedValue;
            // The following Operation Timeout value is given in milliseconds
            DeviceClientOperationTimeout = uint.TryParse(ConfigurationManager.AppSettings["DeviceClientOperationTimeout"], out configuredUnsignedValue) ? configuredUnsignedValue : 60000;
            // The following Receice Timeout value is given in milliseconds
            DeviceClientReceiveTimeout = uint.TryParse(ConfigurationManager.AppSettings["DeviceClientReceiveTimeout"], out configuredUnsignedValue) ? configuredUnsignedValue : 10000;

            // Consider the usage of teh Device Emulator as one session, using only one Guid for logging output
            RequestId = Guid.NewGuid();

            // Switch for C2D receiver mode: true for multiple Device support
            bool configuredSwitchValue;
            bool.TryParse(ConfigurationManager.AppSettings["ReceiceC2DForAll"], out configuredSwitchValue);
            ReceiceC2DForAll = configuredSwitchValue;
        }
#else
        /// <summary>
        /// Default constructor (UWP).
        /// </summary>
        /// <remarks>
        /// Retrieves the IoT Hub relevant connection details from the config file.
        /// </remarks>
        public ConfigurationDto()
        {
            // Try to evaluate desired TransportType for IoT Hub communication
            var localConfigValue = _localSettings.Values[nameof(IotHubTransportType)];
            if (localConfigValue != null)
            {
                DeviceClient.TransportType transportTypeSendConfigured;
                IotHubTransportType = Enum.TryParse((string) localConfigValue, true, out transportTypeSendConfigured) ? transportTypeSendConfigured : DeviceClient.TransportType.Mqtt;
            }
            else
            {
                // MQTT is default
                IotHubTransportType = DeviceClient.TransportType.Mqtt;
            }

            // Get IoT Hub connectivity details from app.config
            IotHubRegistryConnStr = (string)_localSettings.Values[nameof(IotHubRegistryConnStr)];
            HostNameIoT = (string)_localSettings.Values[nameof(HostNameIoT)];

            // For Logging Sink =  Event Hub
            LoggingEventHubName = (string)_localSettings.Values[nameof(LoggingEventHubName)];
            LoggingEventHubConnStr = (string)_localSettings.Values[nameof(LoggingEventHubConnStr)];

            // Get the name of the default Telemetry Sample file to be used for C2D Command
            DefaultRefreshTelemetrySample = (string)_localSettings.Values[nameof(DefaultRefreshTelemetrySample)];

            // Get the current environment
            Environment = (string)_localSettings.Values[nameof(Environment)];

            // Get flag value for Device Certificate usage
            localConfigValue = _localSettings.Values["UseDeviceCertificate"];
            if (localConfigValue != null)
            {
                _useDeviceCertificate = (bool) localConfigValue;
            }

            // Get delay (in secs) to use for receiving C2D Commands and other Timeouts
            localConfigValue = _localSettings.Values[nameof(ReceiceWaitLower)];
            if (localConfigValue != null)
            {
                ReceiceWaitLower = (int) localConfigValue;
            }
            localConfigValue = _localSettings.Values[nameof(ReceiceWaitUpper)];
            if (localConfigValue != null)
            {
                ReceiceWaitUpper = (int)localConfigValue;
            }
            localConfigValue = _localSettings.Values[nameof(MonitoringTimespan)];
            if (localConfigValue != null)
            {
                MonitoringTimespan = (int)localConfigValue;
            }

            // The following Operation Timeout value is given in milliseconds
            localConfigValue = _localSettings.Values[nameof(DeviceClientOperationTimeout)];
            if (localConfigValue != null)
            {
                DeviceClientOperationTimeout = (uint)localConfigValue;
            }
            localConfigValue = _localSettings.Values[nameof(DeviceClientReceiveTimeout)];
            if (localConfigValue != null)
            {
                DeviceClientReceiveTimeout = (uint)localConfigValue;
            }

            // Consider the usage of teh Device Emulator as one session, using only one Guid for logging output
            RequestId = Guid.NewGuid();

            // Switch for C2D receiver mode: true for multiple Device support
            localConfigValue = _localSettings.Values[nameof(ReceiceC2DForAll)];
            if (localConfigValue != null)
            {
                ReceiceC2DForAll = (bool)localConfigValue;
            }

#if WINDOWS_UWP
            // Init sample GeoPositions
            InitSampleGeoPositions();

            // For the time being (no settings dialog), get config data from code
            UpdateConfigIfNotSet();

            // Finally try to evaluate type of Device, this emulator is running on
            EvaluateEmulatorDevice();
#else
            EmulatorDeviceType = EmulatorDeviceTypeEnum.Desktop;
#endif
        }
#endif

        #endregion

        #region Methods

        /// <summary>
        /// Helper method for initializing the configuration store.
        /// </summary>
        /// <remarks>
        /// The following config data is initialized:
        /// <list type="bullet">
        /// <item><description>internal list of supported test devices is prepared, whereby a device will be registered with the IoT Hub if not yet done</description></item>
        /// <item><description>the timestamp tracking per each supported device is initialized with <see cref="DateTime.MinValue"/></description></item>
        /// <item><description>the Geo Location tracking per each supported device is initialized with an empty list</description></item>
        /// <item><description>a list of Alert sample files is prepared</description></item>
        /// </list>
        /// </remarks>
        internal void InitConfigurationData()
        {
            // Init IoT Hub registry management
            _registryManager = RegistryManager.CreateFromConnectionString(IotHubRegistryConnStr);
            Task awaitable = null;

            // If emulator is running on IoT Device, e.g. Raspi, run in single device mode and get IoT details from TPM
            if (EmulatorDeviceType == EmulatorDeviceTypeEnum.IoT)
            {
#if WINDOWS_UWP
                // Run emulator in single device mode and get IoT connectivity details from TPM
                TpmDevice tpmDevice = new TpmDevice(0);
                DeviceConfigurationDto emuDeviceConfig = new DeviceConfigurationDto
                {
                    DeviceId = tpmDevice.GetDeviceId(),
                    CustomerId = "MCS",
                    TimeTracking = DateTime.MinValue,
#if !WINDOWS_UWP
                    GeoTracking = new List<GeoCoordinate>(),
#else
                    GeoTracking = new List<Geopoint>(),
#endif
                    ValueTracking = new Dictionary<string, long>(),
                    DeviceClientOperationTimeout = DeviceClientOperationTimeout,
                    DeviceClientReceiveTimeout = DeviceClientReceiveTimeout,
                    HostNameIoT = tpmDevice.GetHostName(),
                    IoTHubDeviceKey = tpmDevice.GetSASToken()
                };

                // Start registering IoT Hub devices
                awaitable = Task.Run(async () =>
                {
                    await AddDeviceAsync(emuDeviceConfig);
                    // Only add to internal list of supported Devices, if either symmetric key or certificate is available
                    if (!string.IsNullOrEmpty(emuDeviceConfig.IoTHubDeviceKey))
                    {
                        DeviceConfiguration.Add(emuDeviceConfig);
                    }
                });
#endif
            }
            else
            {
                // Get condensed device list from config: separated list of S/N-CustomerId pairs
#if !WINDOWS_UWP
                string configDevices = ConfigurationManager.AppSettings["TestDevices"];
#else
                //string configDevices = (string) _localSettings.Values["TestDevices"];
                string configDevices = "100174355-xxx,100169325-xxx,100171166-xxx,100143502-yyy,100167435-xxx";
#endif
                var deviceList = configDevices.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                // Start registering IoT Hub devices
                awaitable = Task.Run(async () =>
                {
                    await ConfigureDevices(deviceList);
                }); // Task
            }

            // Init other stuff: add list of known Alert sample files for quick access within UI (radio button)
            StatusChangeIngestFile.Add(0, "No Alert (001) - Feature.txt");
            StatusChangeIngestFile.Add(1, "Alert (100) - Fehler 1.txt");
            StatusChangeIngestFile.Add(2, "Alert (200) - Fehler 2.txt");
            StatusChangeIngestFile.Add(3, "Alert (300) - Fehler 3.txt");
            StatusChangeIngestFile.Add(4, "Alert (400) - Fehler 4.txt");
            StatusChangeIngestFile.Add(5, "Alert (500) - Fehler 5.txt");

            // Wait for the device registering - if any - to be finished
            awaitable?.Wait();

            // Close IoT Hub registry management
            _registryManager.CloseAsync().Wait();
        }

        /// <summary>
        /// Helper method for register and configure the Devices to hande by this emulator.
        /// </summary>
        /// <param name="deviceList">List of Devices as defined in emulator config/settings</param>
        /// <returns></returns>
        private async Task ConfigureDevices(List<string> deviceList)
        {
// Loop through all devices configured and init config
            foreach (string device in deviceList)
            {
                // Add each device to internal list of available ones
                string[] deviceDetails = device.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
                // Only init device Id and customer Id for now - other details will be filled later
                DeviceConfigurationDto emuDeviceConfig = new DeviceConfigurationDto
                {
                    DeviceId = deviceDetails[0],
                    CustomerId = deviceDetails[1],
                    TimeTracking = DateTime.MinValue,
#if !WINDOWS_UWP
                        GeoTracking = new List<GeoCoordinate>(),
#else
                    GeoTracking = new List<Geopoint>(),
#endif
                    ValueTracking = new Dictionary<string, long>(),
                    DeviceClientOperationTimeout = DeviceClientOperationTimeout,
                    DeviceClientReceiveTimeout = DeviceClientReceiveTimeout
                };

                // Check if device is already registered on IoT Hub, do so if not - add SAK to internal device data
                emuDeviceConfig.IoTHubDeviceKey = await AddDeviceAsync(emuDeviceConfig);
                emuDeviceConfig.HostNameIoT = HostNameIoT;

                // Any repair due to recent Certificate usage?
                if (!_useDeviceCertificate && string.IsNullOrEmpty(emuDeviceConfig.IoTHubDeviceKey))
                {
                    emuDeviceConfig.DeviceCertificate = null;
                    emuDeviceConfig.IoTHubDeviceKey = await UpdateDeviceAsync(emuDeviceConfig);
                }

                // Check for Device Certificate: if available, update on IoT Hub
#if !WINDOWS_UWP
                    if (_useDeviceCertificate && GetDeviceCertificate(emuDeviceConfig))
#else
                if (_useDeviceCertificate && await GetDeviceCertificate(emuDeviceConfig))
#endif
                {
                    // If no certificate was set on the Device, a new Symmetric key might be returned
                    emuDeviceConfig.IoTHubDeviceKey = await UpdateDeviceAsync(emuDeviceConfig);
                    if (!string.IsNullOrEmpty(emuDeviceConfig.IoTHubDeviceKey))
                    {
                        emuDeviceConfig.DeviceCertificate = null;
                    }
                }

                // Only add to internal list of supported Devices, if either symmetric key or certificate is available
                if (!string.IsNullOrEmpty(emuDeviceConfig.IoTHubDeviceKey) || emuDeviceConfig.DeviceCertificate != null)
                {
                    DeviceConfiguration.Add(emuDeviceConfig);
                }
            } // foreach
        }

        /// <summary>
        /// Helper method for registering the emulator device on IoT Hub if not yet done.
        /// </summary>
        /// <param name="emuDevice">Emulator device config details</param>
        /// <returns>Device specific shared access key, or null if device is disabled</returns>
        private async Task<string> AddDeviceAsync(DeviceConfigurationDto emuDevice)
        {
            Device iotHubDevice;
            try
            {
                // Try to add emulator device, handle case, if already exisitng
                iotHubDevice = await _registryManager.AddDeviceAsync(new Device(emuDevice.DeviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                iotHubDevice = await _registryManager.GetDeviceAsync(emuDevice.DeviceId);
            }

            // Return device specifc SharedAccessKey to update emulatior internal data
            return iotHubDevice?.Status == DeviceStatus.Enabled ? iotHubDevice.Authentication.SymmetricKey.PrimaryKey : null;
        }

        /// <summary>
        /// Helper method for checking whether there is a Device Certificate available under SampleFiles.
        /// </summary>
        /// <param name="emuDeviceConfig">Current Device config</param>
        /// <returns><see langword="true"/> if certificate is available and has been loaded into internal config,<see langword="false"/> otherwise</returns>
        /// <remarks>
        /// If a Device Certificate has been loaded into the internal Device config, it will be used for creating the conection to the IoT Hub.
        /// </remarks>
#if WINDOWS_UWP
        private async Task<bool> GetDeviceCertificate(DeviceConfigurationDto emuDeviceConfig)
#else
        private bool GetDeviceCertificate(DeviceConfigurationDto emuDeviceConfig)
#endif
        {
            bool isAvailable = false;

            // Create full path to possible certificate file
#if WINDOWS_UWP
            StorageFolder appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            string path = $"ms-appx:///Assets/{emuDeviceConfig.DeviceId}.pfx";
            StorageFile certFile = await appFolder.GetFileAsync(path);

            if (certFile != null)
            {
                path = certFile.Path;
#else
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleFiles", "Certificates", string.Concat(emuDeviceConfig.DeviceId, ".pfx"));

            // Check file existence
            if (File.Exists(path))

            {
#endif
                // Device Certificate avaialble, lets load it into internal Device config
                X509Certificate2 cert = new X509Certificate2(path, "P@ssw0rd");
                if (cert.Thumbprint != null)
                {
                    emuDeviceConfig.DeviceCertificate = cert;
                }

                isAvailable = true;
            }

            return isAvailable;
        }

        /// <summary>
        /// Helper method for updating the emulator device on IoT Hub regards of the authentication mechanism.
        /// </summary>
        /// <param name="emuDevice">Emulator device config details</param>
        /// <returns>The Symmetric Key's primary key, if this authentication method was set, otherwise <see langword="null"/></returns>
        private async Task<string> UpdateDeviceAsync(DeviceConfigurationDto emuDevice)
        {
            try
            {
                Device iotHubDevice = await _registryManager.GetDeviceAsync(emuDevice.DeviceId);
                if (emuDevice.DeviceCertificate != null)
                {
                    // Try to add Certificate to Device on IoT HUb side, replacing the Symmetric Key
                    iotHubDevice.Authentication = new AuthenticationMechanism
                    {
                        X509Thumbprint = new X509Thumbprint
                        {
                            PrimaryThumbprint = emuDevice.DeviceCertificate.Thumbprint
                        }
                    };
                }
                else
                {
                    // Try to add Symmetric key to Device on IoT HUb side, replacing the Certificate
                    iotHubDevice.Authentication = new AuthenticationMechanism
                    {
                        SymmetricKey = new SymmetricKey()
                        {
                            PrimaryKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
                            SecondaryKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))
                        }
                    };
                }
                await _registryManager.UpdateDeviceAsync(iotHubDevice, false);
                return iotHubDevice.Authentication.SymmetricKey.PrimaryKey;
            }
            catch (Exception)
            {
                // Ok, lets work with the key instead
                return null;
            }
        }

#if WINDOWS_UWP
        /// <summary>
        /// Helper method for initializing the internal list of prepared Geo Locations.
        /// </summary>
        private void InitSampleGeoPositions()
        {
            // Add sample GeoPosition 0
            BasicGeoposition basicPoint = new BasicGeoposition { Latitude = 47.831850, Longitude = 13.061360 };
            Geopoint geoPoint = new Geopoint(basicPoint);
            SampleGeoCoordinates.Add(0, geoPoint);

            // Add sample GeoPosition 1
            basicPoint = new BasicGeoposition { Latitude = 47.835940, Longitude = 13.055155 };
            geoPoint = new Geopoint(basicPoint);
            SampleGeoCoordinates.Add(1, geoPoint);

            // Add sample GeoPosition 2
            basicPoint = new BasicGeoposition { Latitude = 47.842815, Longitude = 13.070154 };
            geoPoint = new Geopoint(basicPoint);
            SampleGeoCoordinates.Add(2, geoPoint);

            // Add sample GeoPosition 3
            basicPoint = new BasicGeoposition { Latitude = 47.830961, Longitude = 13.082249 };
            geoPoint = new Geopoint(basicPoint);
            SampleGeoCoordinates.Add(3, geoPoint);

            // Add sample GeoPosition 4
            basicPoint = new BasicGeoposition { Latitude = 47.818694, Longitude = 13.106674 };
            geoPoint = new Geopoint(basicPoint);
            SampleGeoCoordinates.Add(4, geoPoint);

            // Add sample GeoPosition 5
            basicPoint = new BasicGeoposition { Latitude = 47.791123, Longitude = 13.069020 };
            geoPoint = new Geopoint(basicPoint);
            SampleGeoCoordinates.Add(5, geoPoint);
        }

        /// <summary>
        /// Helper method for pre-filling any required config values, if not yet set.
        /// </summary>
        /// <remarks>
        /// This is a workaround as long as there is no settings dialog.
        /// </remarks>
        private void UpdateConfigIfNotSet()
        {
            // Get IoT Hub connectivity details from app.config
            if (string.IsNullOrEmpty(IotHubRegistryConnStr))
            {
                IotHubRegistryConnStr = "HostName=PC-DEV-IH-WE-CloudGateway.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=GimnUmsprMFfJkx1nUsE9kI/jEdQjbjHtV+QHi/O3yA=";
            }
            if (string.IsNullOrEmpty(HostNameIoT))
            {
                HostNameIoT = "PC-DEV-IH-WE-CloudGateway.azure-devices.net";
            }

            // For Logging Sink =  Event Hub
            if (string.IsNullOrEmpty(LoggingEventHubName))
            {
                LoggingEventHubName = "pc-dev-eh-we-logging";
            }
            if (string.IsNullOrEmpty(LoggingEventHubConnStr))
            {
                LoggingEventHubConnStr = "Endpoint=sb://pc-dev-eh-we-eventhubs.servicebus.windows.net/;SharedAccessKeyName=SendLogEntries;SharedAccessKey=ZTZLEwQjtgXx/SfjnUf3FlIouTcMuBdziajtcugepHg=;EntityPath=pc-dev-eh-we-logging";
            }

            // Get the name of the default Telemetry Sample file to be used for C2D Command
            if (string.IsNullOrEmpty(DefaultRefreshTelemetrySample))
            {
                DefaultRefreshTelemetrySample = "TelemetryPackage.gz";
            }

            // Get the current environment
            if (string.IsNullOrEmpty(Environment))
            {
                Environment = "DEV";
            }

            // Get delay (in secs) to use for receiving C2D Commands and other Timeouts
            if (ReceiceWaitLower <= 0)
            {
                ReceiceWaitLower = 10;
            }
            if (ReceiceWaitUpper <= 0)
            {
                ReceiceWaitUpper = 16;
            }
            if (MonitoringTimespan <= 0)
            {
                MonitoringTimespan = 15;
            }

            // The following Operation Timeout value is given in milliseconds
            if (DeviceClientOperationTimeout <= 0)
            {
                DeviceClientOperationTimeout = 30000;
            }
            // The following Receice Timeout value is given in milliseconds
            if (DeviceClientReceiveTimeout <= 0)
            {
                DeviceClientReceiveTimeout = 5000;
            }
        }

        /// <summary>
        /// Helper method for persisting the current config details to the App's local settings.
        /// </summary>
        internal void SaveConfigurationData()
        {
            // Try to evaluate desired TransportType for IoT Hub communication
            _localSettings.Values[nameof(IotHubTransportType)] = IotHubTransportType.ToString();

            // Get IoT Hub connectivity details from app.config
            _localSettings.Values[nameof(IotHubRegistryConnStr)] = IotHubRegistryConnStr;
            _localSettings.Values[nameof(HostNameIoT)] = HostNameIoT;

            // For Logging Sink =  Event Hub
            _localSettings.Values[nameof(LoggingEventHubName)] = LoggingEventHubName;
            _localSettings.Values[nameof(LoggingEventHubConnStr)] = LoggingEventHubConnStr;

            // Get the name of the default Telemetry Sample file to be used for C2D Command
            _localSettings.Values[nameof(DefaultRefreshTelemetrySample)] = DefaultRefreshTelemetrySample;

            // Get the current environment
            _localSettings.Values[nameof(Environment)] = Environment;

            // Get flag value for Device Certificate usage
            _localSettings.Values["UseDeviceCertificate"] = _useDeviceCertificate;

            // Get delay (in secs) to use for receiving C2D Commands and other Timeouts
            _localSettings.Values[nameof(ReceiceWaitLower)] = ReceiceWaitLower;
            _localSettings.Values[nameof(ReceiceWaitUpper)]= ReceiceWaitUpper;
            _localSettings.Values[nameof(MonitoringTimespan)]= MonitoringTimespan;

            // The following Operation Timeout value is given in milliseconds
            _localSettings.Values[nameof(DeviceClientOperationTimeout)]= DeviceClientOperationTimeout;
            // The following Receice Timeout value is given in milliseconds
            _localSettings.Values[nameof(DeviceClientReceiveTimeout)] = DeviceClientReceiveTimeout;

            // Switch for C2D receiver mode: true for multiple Device support
            _localSettings.Values[nameof(ReceiceC2DForAll)] = ReceiceC2DForAll;
        }

        /// <summary>
        /// Helper method for evaluating the device type, this emulator is running on.
        /// </summary>
        private void EvaluateEmulatorDevice()
        {
            // Try to evaluate device type
            switch (AnalyticsInfo.VersionInfo.DeviceFamily)
            {
                case "Windows.Mobile":
                    EmulatorDeviceType = EmulatorDeviceTypeEnum.Mobile;
                    break;
                case "Windows.Desktop":
                    EmulatorDeviceType = UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Mouse ? EmulatorDeviceTypeEnum.Desktop : EmulatorDeviceTypeEnum.Tablet;
                    break;
                case "Windows.IoT":
                    EmulatorDeviceType = EmulatorDeviceTypeEnum.IoT;
                    break;
                case "Windows.Team":
                    EmulatorDeviceType = EmulatorDeviceTypeEnum.SurfaceHub;
                    break;
                case "Windows.XBox":
                    EmulatorDeviceType = EmulatorDeviceTypeEnum.XBox;
                    break;
                default:
                    EmulatorDeviceType = EmulatorDeviceTypeEnum.Unknown;
                    break;
            }
        }
#endif

        #endregion
    }
}
