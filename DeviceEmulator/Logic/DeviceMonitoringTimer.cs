using System;
using System.Windows.Threading;

namespace DeviceEmulator.Logic
{
    /// <summary>
    /// Class implementing a Device Monitoring Disptach Timer wrapper for a Device.
    /// </summary>
    /// <remarks>
    /// For each Device handled by the Emulator, a dispatch timer will be setup and started on specific C2D Monitoring Command received.
    /// The dispatch timer will fire every x secs (as configured) and sent out a single Telemetry message.
    /// </remarks>
    public class DeviceMonitoringTimer
    {
        #region Members

        private readonly IEmulatorLogic _emuLogic;
        private readonly string _deviceId;
        private readonly DispatcherTimer _monitoringTimer;

        #endregion

        #region Properties

        /// <summary>
        /// Property for the dispatch timer state (en/disabled).
        /// </summary>
        /// <value>Gets the current dispatch timer state</value>
        public bool IsTimerRunning => _monitoringTimer.IsEnabled;

        #endregion

        #region Constructor

        /// <summary>
        /// Overloaded constructor.
        /// </summary>
        /// <param name="emuLogic">Reference to Device Emulator Business Logic</param>
        /// <param name="deviceId">Device Id to handle in this instance</param>
        public DeviceMonitoringTimer(IEmulatorLogic emuLogic, string deviceId)
        {
            // Init internal members with param values
            _emuLogic = emuLogic;
            _deviceId = deviceId;

            // Setup timer
            _monitoringTimer = new DispatcherTimer();
            _monitoringTimer.Tick += MonitoringTimer_Tick;
            _monitoringTimer.Interval = TimeSpan.FromSeconds(_emuLogic.Configuration.MonitoringTimespan);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method for starting the Device Monitoring Timer of this instance.
        /// </summary>
        public void StartMonitoringTimer()
        {
            if (!_monitoringTimer.IsEnabled)
            {
                _monitoringTimer.Start();
            }
        }

        /// <summary>
        /// Method for stopping the Device Monitoring Timer of this instance.
        /// </summary>
        public void StopMonitoringTimer()
        {
            if (_monitoringTimer.IsEnabled)
            {
                _monitoringTimer.Stop();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Event handler for dispatch timer tick.
        /// </summary>
        /// <param name="sender">Event sender (Dispatch Timer)</param>
        /// <param name="e">Event params</param>
        /// <remarks>
        /// Event handler triggers check for possibly received C2D Command.
        /// </remarks>
        private async void MonitoringTimer_Tick(object sender, EventArgs e)
        {
            // Stopp timer of this instance to be able to process
            _monitoringTimer.Stop();

            // Find device config for device handeld by this instance
            int idx = _emuLogic.Configuration.DeviceConfiguration.FindIndex(configurationDto => configurationDto.DeviceId.Equals(_deviceId));
            if (_emuLogic.Configuration.DeviceConfiguration != null && _emuLogic.Configuration.DeviceConfiguration.Count > idx && idx >= 0)
            {
                // Try to look for C2D Command message for currently selected device
                await _emuLogic.SendMonitoringTelemetryAsync(_emuLogic.Configuration.DeviceConfiguration[idx]);
            }

            // Restart timer of this instance
            _monitoringTimer.Start();
        }

        #endregion
    }
}
