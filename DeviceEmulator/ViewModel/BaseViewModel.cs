using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeviceEmulator.ViewModel
{
    /// <summary>
    /// Class implementing a base ViewModel.
    /// </summary>
    /// <remarks>
    /// All implemented ViewModels should be derived from this one.
    /// </remarks>
    public class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event handler definition.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// OnPropertyChanged event handler implementation.
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
