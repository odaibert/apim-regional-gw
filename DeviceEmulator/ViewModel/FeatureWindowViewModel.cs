using System.Collections.Generic;
using Common.Dto;

namespace DeviceEmulator.ViewModel
{
    /// <summary>
    /// Class implementing the ViewModel of the FeatureWindow.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class FeatureWindowViewModel: BaseViewModel
    {
        #region Members

        private List<DeviceFeatureDto> _deviceFeatures = new List<DeviceFeatureDto>();

        #endregion

        #region Properties

        /// <summary>
        /// Property for the internal list of Telemetry sample file details.
        /// </summary>
        /// <value>Gets or sets the internal list of Telemetry sample file details</value>
        /// <remarks>
        /// </remarks>
        public List<DeviceFeatureDto> DeviceFeatures
        {
            get { return _deviceFeatures; }
            set
            {
                _deviceFeatures = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Constructor

        #endregion

        #region Private Methods

        #endregion
    }
}
