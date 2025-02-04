using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
// Own usings
using Common.Dto;
using Common.Enums;
using DeviceEmulator.Dto;
using DeviceEmulator.Logic;
using DeviceEmulator.Logic.Event;
using static System.String;

namespace DeviceEmulator.ViewModel
{
    /// <summary>
    /// Class implementing the ViewModel of the MainWindow.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class MainWindowViewModel : BaseViewModel
    {
        #region Member(s)

        private readonly IEmulatorLogic _emuLogic;
        private ConfigurationDto _configuration = new ConfigurationDto();
        private const string TelemetryFilesFolder = "SampleFiles";

        //DispatcherTimer receiveCommandTimer;

        private string _displayStatus = "";
        private string _arrivedCommandsStatus = "";
        private int _indexSelectedTelemetryIngestFile;
        private int _indexSelectedMachine;
        private List<TelemetrySampleFileDto> _telemetryIngestFiles = new List<TelemetrySampleFileDto>();

        #endregion

        #region Properties

        /// <summary>
        /// Property for the Device Emulator configuration details.
        /// </summary>
        /// <value>Gets or sets the Device Emulator configuration details</value>
        public ConfigurationDto Configuration
        {
            get { return _configuration; }
            set
            {
                _configuration = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property for the general UI status display.
        /// </summary>
        /// <value>Gets or sets the general UI status display</value>
        /// <remarks>
        /// The status is displayed within a textbox.
        /// </remarks>
        public string DisplayStatus
        {
            get { return _displayStatus; }
            set
            {
                _displayStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property for the C2D Command UI status display.
        /// </summary>
        /// <value>Gets or sets the C2D Command UI status display</value>
        /// <remarks>
        /// The status is displayed within a textbox.
        /// </remarks>
        public string ArrivedCommandsStatus
        {
            get { return _arrivedCommandsStatus; }
            set
            {
                _arrivedCommandsStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property for the currently selected Telemetry sample file.
        /// </summary>
        /// <value>Gets or sets the currently selected Telemetry sample file</value>
        /// <remarks>
        /// The currently selected Telemetry sample file is selectable through a dropdown listbox and contains filenames.
        /// </remarks>
        public int IndexSelectedTelemetryIngestFile
        {
            get { return _indexSelectedTelemetryIngestFile; }
            set
            {
                _indexSelectedTelemetryIngestFile = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property for the currently selected device.
        /// </summary>
        /// <value>Gets or sets the currently selected device</value>
        /// <remarks>
        /// The currently selected device is selectable through a dropdown listbox and contains device Ids.
        /// </remarks>
        public int IndexSelectedMachine
        {
            get { return _indexSelectedMachine; }
            set
            {
                _indexSelectedMachine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property for the internal list of Telemetry sample file details.
        /// </summary>
        /// <value>Gets or sets the internal list of Telemetry sample file details</value>
        /// <remarks>
        /// </remarks>
        public List<TelemetrySampleFileDto> TelemetryIngestFiles
        {
            get { return _telemetryIngestFiles; }
            set
            {
                _telemetryIngestFiles = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property for the current C2D Command receive status.
        /// </summary>
        /// <value>Gets or sets the current C2D Command receive status</value>
        public bool IsC2DCommandReceiveEnabled { get; set;  } = false;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="emuLogic">Reference to emulator logic</param>
        /// <remarks>
        /// Inits the following: emulator config store with details about testable devices.
        /// </remarks>
        public MainWindowViewModel(IEmulatorLogic emuLogic)
        {
            // Add reference to business logic
            _emuLogic = emuLogic;

            // Initialize the emulator internal config data, incl. registering devices if necessary
            // ...also device specifc tracking for timestamps and GEO locations is initialized
            _configuration.InitConfigurationData();
            _emuLogic.Configuration = _configuration;

            // Prepare events: one for C2D Command arrival, one for UI status updates
            _emuLogic.C2DCommandArrived += EmuLogicC2DCommandArrived;
            _emuLogic.StatusUpdateChange += EmuLogicStatusUpdateChange;

            // Prepare Dispatch Timer for each device, but do not yet start the timer
            foreach (DeviceConfigurationDto configurationDto in _configuration.DeviceConfiguration)
            {
                _emuLogic.DeviceC2DDispatcher.Add(configurationDto.DeviceId, new C2DCommandTimer(_emuLogic, configurationDto.DeviceId));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method for simulating a Device Feature usage, i.e. sending out a specifc Telemetry Message.
        /// </summary>
        /// <param name="featureDto">Device Feature details</param>
        /// <returns></returns>
        public async Task<bool> SimulateDeviceFeatureUsage(DeviceFeatureDto featureDto)
        {
            // Get device config
            DeviceConfigurationDto deviceConfiguration = _configuration.DeviceConfiguration[IndexSelectedMachine];
            bool isSuccessfullyUsed = await _emuLogic.ProcessFeatureUsagetAsync(0, deviceConfiguration, featureDto);

            return isSuccessfullyUsed;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Helper method for retrieving the list of device features.
        /// </summary>
        /// <returns>Count of retrieved features</returns>
        internal async Task<List<DeviceFeatureDto>> GetDeviceFeatures()
        {
            // Get config of currently selected device and create cleint if necessary
            DeviceConfigurationDto deviceConfigDto = _configuration.DeviceConfiguration[IndexSelectedMachine];
            if (deviceConfigDto.DeviceClient == null)
            {
                deviceConfigDto.DeviceClient = await _emuLogic.GetDeviceClient(deviceConfigDto, _configuration.IotHubTransportType, true);
            }

            await _emuLogic.GetDeviceFeaturesFromTwin(deviceConfigDto);

            return deviceConfigDto.FeatureTracking;
        }

        /// <summary>
        /// Helper method for starting/stopping the Dispatch Timer associated with a Device.
        /// </summary>
        internal async Task StartStopC2DCommandReceiving()
        {
            if (IsC2DCommandReceiveEnabled)
            {
                // Start the dispatch timer for each device
                if (_configuration.ReceiceC2DForAll)
                {
                    // Enable Dispatch Timer for all handled devices
                    foreach (DeviceConfigurationDto configurationDto in _configuration.DeviceConfiguration)
                    {
                        if (configurationDto.DeviceClient == null)
                        {
                            configurationDto.DeviceClient = await _emuLogic.GetDeviceClient(configurationDto, _configuration.IotHubTransportType, true);
                        }
                        _emuLogic.DeviceC2DDispatcher[configurationDto.DeviceId].StartDeviceTimer();
                    }
                }
                else
                {
                    DeviceConfigurationDto deviceConfigDto = _configuration.DeviceConfiguration[IndexSelectedMachine];
                    if (deviceConfigDto.DeviceClient == null)
                    {
                        deviceConfigDto.DeviceClient = await _emuLogic.GetDeviceClient(deviceConfigDto, _configuration.IotHubTransportType, true);
                    }

                    // Only enable Dispatch Timer for currently selected Device Id in the UI
                    string deviceId = deviceConfigDto.DeviceId;
                    _emuLogic.DeviceC2DDispatcher[deviceId].StartDeviceTimer();
                }
            }
            else
            {
                if (_configuration.ReceiceC2DForAll)
                {
                    // Stop the C2D Command dispatch timer for each device, also stop the Device Monitoring dispatch timer
                    _configuration.DeviceConfiguration.ForEach(configurationDto =>
                    {
                        if (configurationDto.MonitoringDispatcher != null)
                        {
                            configurationDto.MonitoringDispatcher.StopMonitoringTimer();
                            configurationDto.MonitoringDispatcher = null;
                        }
                    });
                    _configuration.DeviceConfiguration.ForEach(configurationDto => _emuLogic.DeviceC2DDispatcher[configurationDto.DeviceId].StopDeviceTimer());
                }
                else
                {
                    // Only stop the C2D Command dispatch timer for currently selected Device Id in the UI, do same for the Device Monitoring dispatch timer
                    DeviceConfigurationDto configurationDto = _configuration.DeviceConfiguration[IndexSelectedMachine];
                    if (configurationDto.MonitoringDispatcher != null)
                    {
                        configurationDto.MonitoringDispatcher.StopMonitoringTimer();
                        configurationDto.MonitoringDispatcher = null;
                    }
                    _emuLogic.DeviceC2DDispatcher[configurationDto.DeviceId].StopDeviceTimer();
                }
            }
        }

        /// <summary>
        /// Helper method for starting/stopping the Dispatch Timer associated with a Device.
        /// </summary>
        internal async Task SwitchC2DCommandReceiving(DeviceConfigurationDto prevConfigurationDto, DeviceConfigurationDto nextConfigurationDto )
        {
            // We only have to do soemthing, if receiving is currently enabled
            if (IsC2DCommandReceiveEnabled)
            {
                // ...and we only have to take care, if single receive mode is active
                if (!_configuration.ReceiceC2DForAll)
                {
                    // Stop Dispatch Timer for previously selected Device
                    if (prevConfigurationDto?.DeviceClient != null)
                    {
                        // Only enable Dispatch Timer for currently selected Device Id in the UI
                        string deviceId = prevConfigurationDto.DeviceId;
                        _emuLogic.DeviceC2DDispatcher[deviceId].StopDeviceTimer();
                    }

                    // Start Dispatch Timer for newly selected Device
                    if (nextConfigurationDto != null)
                    {
                        if (nextConfigurationDto.DeviceClient == null)
                        {
                            // Get Device Client reference
                            nextConfigurationDto.DeviceClient = await _emuLogic.GetDeviceClient(nextConfigurationDto, _configuration.IotHubTransportType, true);
                        }

                        if (nextConfigurationDto.DeviceClient != null)
                        {
                            // Only enable Dispatch Timer for currently selected Device Id in the UI
                            string deviceId = nextConfigurationDto.DeviceId;
                            _emuLogic.DeviceC2DDispatcher[deviceId].StartDeviceTimer();
                        }
                    }
                }
            }
        }

#if !WINDOWS_UWP
        /// <summary>
        /// Helper method for retrieving the list of available sample Telemetry/NRT/Alerts files.
        /// </summary>
        /// <returns>Count of retrieved sample files.</returns>
        internal bool GetTelemetryFileNames()
        {
            TelemetryIngestFiles = _emuLogic.GetSampleDataFileNames(TelemetryFilesFolder);
            return TelemetryIngestFiles.Count > 0;
        }
#else
        /// <summary>
        /// Helper method for retrieving the list of available sample Telemetry/NRT/Alerts files.
        /// </summary>
        /// <returns>Count of retrieved sample files.</returns>
        internal async Task<bool> GetTelemetryFileNames()
        {
            TelemetryIngestFiles = await _emuLogic.GetSampleDataFileNames(TelemetryFilesFolder);
            if (TelemetryIngestFiles.Count > 0)
            {
                IndexSelectedTelemetryIngestFile = 0;
                return true;
            }
            return false;
        }
#endif

        /// <summary>
        /// Helper method for preparing the ingest of Telemetry/NRT/Alert message(s) file.
        /// </summary>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otheriwse</returns>
        internal async Task<bool> IngestTelemetryFile()
        {
            // Get config of currently selected device and create cleint if necessary
            DeviceConfigurationDto deviceConfigDto = _configuration.DeviceConfiguration[IndexSelectedMachine];
            if (deviceConfigDto.DeviceClient == null)
            {
                deviceConfigDto.DeviceClient = await _emuLogic.GetDeviceClient(deviceConfigDto, _configuration.IotHubTransportType, true);
            }

            return await _emuLogic.IngestTelemetryAsync(TelemetryIngestFiles.ElementAt(IndexSelectedTelemetryIngestFile), deviceConfigDto);
        }

        /// <summary>
        /// Helper method for preparing the ingest of an Alert message(s).
        /// </summary>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otheriwse</returns>
        internal async Task<bool> IngestAlert(int statusIndex)
        {
            // Get config of currently selected device and create cleint if necessary
            DeviceConfigurationDto deviceConfigDto = _configuration.DeviceConfiguration[IndexSelectedMachine];
            if (deviceConfigDto.DeviceClient == null)
            {
                deviceConfigDto.DeviceClient = await _emuLogic.GetDeviceClient(deviceConfigDto, _configuration.IotHubTransportType, true);
            }

            return await _emuLogic.IngestAlertAsync(statusIndex, deviceConfigDto);
        }

#endregion

        #region Private Methods

        /// <summary>
        /// Event handler for a received C2D Command.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event params</param>
        /// <remarks>
        /// Event handler updates command receice status in UI and process the C2D Command received.
        /// </remarks>
        private async Task EmuLogicC2DCommandArrived(object sender, C2DCommandEventArgs e)
        {
            // Update UI
            ArrivedCommandsStatus = $"{DateTime.Now.ToString(CultureInfo.InvariantCulture)}: {e.DeviceConfiguration.DeviceId}: {e.DeviceCommandMessage.CommandType} - {e.DeviceCommandMessage.CommandDescription}";

            // Process the C2D Command
            await ProcessCustomCommand(e.DeviceConfiguration, e.DeviceCommandMessage);
        }

        /// <summary>
        /// Event handler for a status update.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event params</param>
        private void EmuLogicStatusUpdateChange(object sender, StatusChangeEventArgs e)
        {
            // Just update the status in the UI
            DisplayStatus = $"Status: {e.DisplayStatus}";
        }

        /// <summary>
        /// Helper method for processing received C2D Commands from the IoT Hub.
        /// </summary>
        /// <param name="deviceConfiguration">Device config deatils</param>
        /// <param name="deviceCommand">C2D Command message received</param>
        /// <remarks>
        /// Currently only the following C2D Command(s) are handled:
        /// <list type="bullet">
        /// <item><description><see cref="C2DCommandType.RequestTelemetryPackage"/> - requesting Telemetry Data Package from Device</description></item>
        /// <item><description><see cref="C2DCommandType.EnDisableFeature"/> - processing Device Feature en/disable</description></item>
        /// </list>
        /// </remarks>
        private async Task ProcessCustomCommand(DeviceConfigurationDto deviceConfiguration, DeviceCommandMessageDto deviceCommand)
        {
            // Get C2D Command Type
            C2DCommandType commandType;
            if (Enum.TryParse(deviceCommand.CommandType, true, out commandType))

            // Evaluate C2D Command
            switch (commandType)
            {
                // "Fresh" Telemetry data has been requested from the device
                case C2DCommandType.RequestSingleTelemetry:
                    // Not yet supported
                    break;
                case C2DCommandType.RequestTelemetryPackage:
                    // Trigger sending Device Telemetry Package
                    await RefreshDeviceTelemetryDataAsync(deviceConfiguration);
                    break;
                case C2DCommandType.EnDisableFeature:
                    // Process feature en/disable C2D Command
                    await EnDsiableDeviceFeatureAsync(deviceConfiguration, deviceCommand);
                    break;
                case C2DCommandType.StartMonitoring:
                case C2DCommandType.StopMonitoring:
                    // Process Device Monitoring Start/Stop C2D Command
                    EnDisableDeviceMonitoring(deviceConfiguration, deviceCommand);
                    break;
                default:
                    // Unknown or not supported C2D Command
                    DisplayStatus = $"Unknown C2D Command Type {deviceCommand.CommandType}";
                    break;
            }
        }

        /// <summary>
        /// Method for simulating the Telemetry Data Refresh triggered by the C2D Command initiated from the client UI.
        /// </summary>
        /// <param name="deviceConfiguration">Device configuration details</param>
        private async Task RefreshDeviceTelemetryDataAsync(DeviceConfigurationDto deviceConfiguration)
        {
            // Get default Telemetry sample file
#if !WINDOWS_UWP
            TelemetrySampleFileDto telemetrySampleFileDto = TelemetryIngestFiles.SingleOrDefault(sampleFileDto => Compare(sampleFileDto.FileName, _configuration.DefaultRefreshTelemetrySample, StringComparison.InvariantCultureIgnoreCase) == 0);
#else
            TelemetrySampleFileDto telemetrySampleFileDto = TelemetryIngestFiles.SingleOrDefault(sampleFileDto => Compare(sampleFileDto.FileName, _configuration.DefaultRefreshTelemetrySample, StringComparison.CurrentCultureIgnoreCase) == 0);
#endif

            // Ingest Telemetry data due to C2D Command
            await _emuLogic.IngestTelemetryAsync(telemetrySampleFileDto, deviceConfiguration);
        }

        /// <summary>
        /// Method for simulating a feature en/disable triggered by the C2D Command initiated from the client UI.
        /// </summary>
        /// <param name="deviceConfiguration">Device configuration details</param>
        /// <param name="deviceCommand">C2D Command Message</param>
        private async Task EnDsiableDeviceFeatureAsync(DeviceConfigurationDto deviceConfiguration, DeviceCommandMessageDto deviceCommand)
        {
            // Ingest Telemetry data due to C2D Command
            await _emuLogic.ProcessFeatureEnDisable(deviceConfiguration, deviceCommand);
        }

        /// <summary>
        /// Method for handling the Device Monitoring start/stop, triggered by a specific C2D Command initiated from the client UI.
        /// </summary>
        /// <param name="deviceConfiguration">Device configuration details</param>
        /// <param name="deviceCommand">Monitoring C2D Command Message</param>
        /// <remarks>
        /// Based on the C2D Command, the method will eitehr start or stop the Monitoring Dispatch Timer for the Device.
        /// </remarks>
        private void EnDisableDeviceMonitoring(DeviceConfigurationDto deviceConfiguration, DeviceCommandMessageDto deviceCommand)
        {
            // Check on specific C2D Command again
            C2DCommandType commandType;
            if (Enum.TryParse(deviceCommand.CommandType, true, out commandType))
            {
                if (commandType == C2DCommandType.StartMonitoring)
                {
                    // Create Device Monitoring Dispatch Timer
                    deviceConfiguration. MonitoringDispatcher = new DeviceMonitoringTimer(_emuLogic, deviceConfiguration.DeviceId);
                    //deviceConfiguration.MonitoringDispatcher.StartMonitoringTimer();
                }
                else if (commandType == C2DCommandType.StopMonitoring)
                {
                    // Stop and remove Deveice Monitoring Dispatch Timer
                    if (deviceConfiguration.MonitoringDispatcher != null && deviceConfiguration.MonitoringDispatcher.IsTimerRunning)
                    {
                        deviceConfiguration.MonitoringDispatcher.StopMonitoringTimer();
                    }
                    deviceConfiguration.MonitoringDispatcher = null;
                }
                else
                {
                    DisplayStatus = $"Unsupported C2D Command Type {deviceCommand.CommandType} for Monitoring encountered!";
                }
            }
            else
            {
                DisplayStatus = $"Unknown C2D Command Type {deviceCommand.CommandType} for Monitoring encountered!";
            }
        }

        #endregion
    }
}
