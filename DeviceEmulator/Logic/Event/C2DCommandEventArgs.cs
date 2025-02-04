using Common.Dto;
using DeviceEmulator.Dto;

namespace DeviceEmulator.Logic.Event
{
    /// <summary>
    /// Class implementing the C2DCommandEvent Args.
    /// </summary>
    public class C2DCommandEventArgs
    {
        /// <summary>
        /// Property for the Device Configuration as argument for the <see cref="EmulatorLogic.C2DCommandEventHandler"/>.
        /// </summary>
        /// <value>Gets or sets the Device Configuratio</value>
        public DeviceConfigurationDto DeviceConfiguration { get; set; }

        /// <summary>
        /// Property for the Device Command Message as argument for the <see cref="EmulatorLogic.C2DCommandEventHandler"/>.
        /// </summary>
        /// <value>Gets or sets the Device Command Message</value>
        public DeviceCommandMessageDto DeviceCommandMessage { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="deviceConfiguration">Device config details to carry as event arg</param>
        /// <param name="deviceCommandMessage">Device Command Message details to carry as event arg</param>
        public C2DCommandEventArgs(DeviceConfigurationDto deviceConfiguration, DeviceCommandMessageDto deviceCommandMessage)
        {
            DeviceConfiguration = deviceConfiguration;
            DeviceCommandMessage = deviceCommandMessage;
        }
    }
}
