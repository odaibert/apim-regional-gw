using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DeviceEmulator.Dto;
using DeviceEmulator.Logic;
using DeviceEmulator.ViewModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DeviceEmulator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Member(s)

        // Recerence to ViewModel of MainWindow
        readonly MainWindowViewModel _viewModel = new MainWindowViewModel(new EmulatorLogic());

        #endregion

        #region Properties

        #endregion

        #region Constructor

        public MainPage()
        {
            // Set-up event handlers
            Loaded += MainPage_Loaded;

            // Init window stuff
            InitializeComponent();
            GrdMain.DataContext = _viewModel;

            // Init some content
            EnvironmentLbl.Text = _viewModel.Configuration.Environment;
            EmuDeviceLbl.Text = $"({_viewModel.Configuration.EmulatorDeviceType})";
            ReceiceCommandsBtn.Content = _viewModel.Configuration.ReceiceC2DForAll ? "Start Receiving (M)" : "Start Receiving (S)";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method for handling the loaded event of the MainWindow
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        /// <remarks>
        /// Handler initializes the list of available telemetry/NRT/Alerts files.
        /// </remarks>
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.GetTelemetryFileNames();
        }

        ///// <summary>
        ///// Method for handling the loaded event of the MainWindow
        ///// </summary>
        ///// <param name="sender">Event sender</param>
        ///// <param name="e">Event args</param>
        ///// <remarks>
        ///// Handler initializes the list of available telemetry/NRT/Alerts files.
        ///// </remarks>
        //private async void MainPage_Closed(object sender, EventArgs e)
        //{
        //    // Clean-up of Device Clients: close and dispose
        //    foreach (DeviceConfigurationDto configurationDto in _viewModel.Configuration.DeviceConfiguration)
        //    {
        //        if (configurationDto.DeviceClient != null)
        //        {
        //            try
        //            {
        //                await configurationDto.DeviceClient.CloseAsync();
        //                configurationDto.DeviceClient?.Dispose();
        //            }
        //            catch (Exception)
        //            {
        //                configurationDto.DeviceClient = null;
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Handler for the Click event for ingesting Telemetry/NRT/Alert.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event param</param>
        private async void IngestTelemetry_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.IngestTelemetryFile();
        }

        private async void StatusChanges_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton selectedRadioButton = (sender as RadioButton);
            if (selectedRadioButton != null)
            {
                var statusIndex = Convert.ToInt32(selectedRadioButton.Tag);
                await _viewModel.IngestAlert(statusIndex);
            }
        }

        /// <summary>
        /// Handler for the Click event for clearing the display status and command controls.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event param</param>
        private void ClearStatus_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DisplayStatus = "";
            _viewModel.ArrivedCommandsStatus = "";
        }

        /// <summary>
        /// Handler for the Click event for start/stop of receiving C2D Commands.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event param</param>
        private async void ReceiveCommands_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsC2DCommandReceiveEnabled)
            {
                _viewModel.IsC2DCommandReceiveEnabled = false;
                ReceiceCommandsBtn.Content = _viewModel.Configuration.ReceiceC2DForAll ? "Start Receiving (M)" : "Start Receiving (S)";
            }
            else
            {
                _viewModel.IsC2DCommandReceiveEnabled = true;
                ReceiceCommandsBtn.Content = _viewModel.Configuration.ReceiceC2DForAll ? "Stop Receiving (M)" : "Stop Receiving (S)";
            }
            await _viewModel.StartStopC2DCommandReceiving();
        }

        /// <summary>
        /// Handler for the SelectionChanged event, to switch for start/stop of receiving C2D Commands.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event param</param>
        private async void SelectedMachine_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceConfigurationDto prevDeviceConfig = null;
            DeviceConfigurationDto nextDeviceConfig = null;
            if (e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                prevDeviceConfig = e.RemovedItems[0] as DeviceConfigurationDto;
            }
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                nextDeviceConfig = e.AddedItems[0] as DeviceConfigurationDto;
            }

            await _viewModel.SwitchC2DCommandReceiving(prevDeviceConfig, nextDeviceConfig);
        }

        /// <summary>
        /// Handler for the Click event for showing the device feature set.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event param</param>
        private async void ShowFeature_Click(object sender, RoutedEventArgs e)
        {
            //ShowFeaturesBtn.IsEnabled = false;
            //// Get current Device Feature List
            //List<DeviceFeatureDto> deviceFeatureList = await _viewModel.GetDeviceFeatures();

            //// Create Feature Window and show a modal dialog
            //FeatureWindow featureWindow = new FeatureWindow(deviceFeatureList) { MainViewModel = _viewModel };
            //featureWindow.ShowDialog();
            //ShowFeaturesBtn.IsEnabled = true;
        }

    #endregion
    
    }
}
