using System;
#if !WINDOWS_UWP
using System.Windows.Threading;
#else
using Windows.UI.Xaml;
#endif

namespace DeviceEmulator.Logic
{
    /// <summary>
    /// Class implementing a C2D Command Dispatch Timer wrapper for a Device.
    /// </summary>
    /// <remarks>
    /// For each Device handled by the Emulator, a dispatch time will be setup and started on specific button click.
    /// The dispatch timer will fire every x secs (random value from configured range) and check, whether there is a C2D Command
    /// waiting for the Device at the IoT Hub.
    /// If so, the time will be stopped, the C2D Command will be processed and the dispatch timer will be started all over again.
    /// </remarks>
    public class C2DCommandTimer
    {
#region Members

        private readonly IEmulatorLogic _emuLogic;
        private readonly string _deviceId;
        private readonly DispatcherTimer _commandReceiveTimer;

#endregion

#region Properties

        /// <summary>
        /// Property for the dispatch timer state (en/disabled).
        /// </summary>
        /// <value>Gets the current dispatch timer state</value>
        public bool IsTimerRunning => _commandReceiveTimer.IsEnabled;

#endregion

#region Constructor

        /// <summary>
        /// Overloaded constructor.
        /// </summary>
        /// <param name="emuLogic">Reference to Device Emulator Business Logic</param>
        /// <param name="deviceId">Device Id to handle in this instance</param>
        public C2DCommandTimer(IEmulatorLogic emuLogic, string deviceId)
        {
            // Init internal members with param values
            _emuLogic = emuLogic;
            _deviceId = deviceId;

            Random randomGenerator = new Random(DateTime.Now.Millisecond);
            int intervalInSecs = randomGenerator.Next(_emuLogic.Configuration.ReceiceWaitLower, _emuLogic.Configuration.ReceiceWaitUpper);

            // Setup timer
            _commandReceiveTimer = new DispatcherTimer();
            _commandReceiveTimer.Tick += ReceiveCommandTimer_Tick;
            _commandReceiveTimer.Interval = TimeSpan.FromSeconds(intervalInSecs);
            //_commandReceiveTimer.Start();
        }

#endregion

#region Public Methods

        /// <summary>
        /// Method for starting the Dispatch Timer of this instance.
        /// </summary>
        public void StartDeviceTimer()
        {
            if (!_commandReceiveTimer.IsEnabled)
            {
                _commandReceiveTimer.Start();
            }
        }

        /// <summary>
        /// Method for stopping the Dispatch Timer of this instance.
        /// </summary>
        public void StopDeviceTimer()
        {
            if (_commandReceiveTimer.IsEnabled)
            {
                _commandReceiveTimer.Stop();
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
#if !WINDOWS_UWP
        private async void ReceiveCommandTimer_Tick(object sender, EventArgs e)
#else
        private async void ReceiveCommandTimer_Tick(object sender, object e)
#endif
        {
            // Stopp timer of this instance to be able to process
            _commandReceiveTimer.Stop();

            // Find device config for device handeld by this instance
            int idx = _emuLogic.Configuration.DeviceConfiguration.FindIndex(configurationDto => configurationDto.DeviceId.Equals(_deviceId));
            if (_emuLogic.Configuration.DeviceConfiguration != null && _emuLogic.Configuration.DeviceConfiguration.Count > idx && idx >= 0)
            {
                // Try to look for C2D Command message for currently selected device
                await _emuLogic.CheckForCommandAsync(_emuLogic.Configuration.DeviceConfiguration[idx]);
            }

            // Restart timer of this instance
            _commandReceiveTimer.Start();
        }

#endregion
    }
}
