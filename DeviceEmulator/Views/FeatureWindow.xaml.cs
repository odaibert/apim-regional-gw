using System.Collections;
using System.Collections.Generic;
using System.Windows;
// Own usings
using Common.Dto;
using DeviceEmulator.ViewModel;

namespace DeviceEmulator.Views
{
    /// <summary>
    /// Interaction logic for FeatureWindow.xaml
    /// </summary>
    public partial class FeatureWindow
    {
        #region Member(s)

        // Recerence to ViewModel of MainWindow
        readonly FeatureWindowViewModel _viewModel = new FeatureWindowViewModel();

        #endregion

        #region Properties

        /// <summary>
        /// Property for the ViewModel reference of the MainWindow.
        /// </summary>
        /// <value>Gets or sets the reference of the ViewModel of the MainWindow</value>
        public MainWindowViewModel MainViewModel { get; set; }

        #endregion
        
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <remarks>
        /// Initialize View and ViewModel.
        /// </remarks>
        public FeatureWindow()
        {
            Loaded += FeatureWindow_Loaded;
            InitializeComponent();
            _viewModel.DeviceFeatures = null;
            GrdFeature.DataContext = _viewModel;
            Owner = Application.Current.MainWindow;
            Title = "Azure IoT Hub";
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <remarks>
        /// Initialize View and ViewModel.
        /// </remarks>
        public FeatureWindow(List<DeviceFeatureDto> deviceFeatureList)
        {
            Loaded += FeatureWindow_Loaded;
            InitializeComponent();
            _viewModel.DeviceFeatures = deviceFeatureList;
            GrdFeature.DataContext = _viewModel;
            Owner = Application.Current.MainWindow;
            Title = "Azure IoT Hub";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method for handling the loaded event of the FeatureWindow
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        /// <remarks>
        /// Handler initializes the list of device features.
        /// </remarks>
        private void FeatureWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Handler for the Click event for closing the device feature set dialog.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event param</param>
        private void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        #endregion

        /// <summary>
        /// Event handler for the feature button click.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        private async void ButtonFeatureUseClick(object sender, RoutedEventArgs e)
        {
            // Get selected items from listbox: only one selected item is supported
            IList selectedFeatures = ListBoxFeatures.SelectedItems;

            if (selectedFeatures.Count > 0)
            {
                // Get feature details from selection in listbox
                DeviceFeatureDto featureDto = selectedFeatures[0] as DeviceFeatureDto;

                if (featureDto == null)
                {
                    return;
                }

                // Simulate feature usage by sending speciic Telemetry Message
                await MainViewModel.SimulateDeviceFeatureUsage(featureDto);

                // Refresh Feature list
                _viewModel.DeviceFeatures = await MainViewModel.GetDeviceFeatures();
            }
        }
    }
}
