using System;

namespace DeviceEmulator.Logic.Event
{
    /// <summary>
    /// Class implementing the StatusChangeEvent Args.
    /// </summary>
    public class StatusChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Property for the display status of the StatusChangeEvent.
        /// </summary>
        /// <value>Gets or sets display status</value>
        public string DisplayStatus { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="displayStatus">Display status to set</param>
        public StatusChangeEventArgs(string displayStatus)
        {
            DisplayStatus = displayStatus;
        }
    }
}
