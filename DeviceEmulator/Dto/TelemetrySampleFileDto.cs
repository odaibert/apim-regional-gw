using Common.Enums;

namespace DeviceEmulator.Dto
{
    /// <summary>
    /// Class implementing the DTO for a Telemetry Sample Data file.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class TelemetrySampleFileDto
    {
        #region Properties

        /// <summary>
        /// Property for the Ingest Message Type.
        /// </summary>
        /// <value>Gets or sets the Ingest Message Type</value>
        public IngestMessageType MessageType { get; set; }
        /// <summary>
        /// Property for the Telemetry sample file name.
        /// </summary>
        /// <value>Gets or sets the Telemetry sample file name</value>
        public string FileName { get; set; }
        /// <summary>
        /// Property for the Telemetry sample file path.
        /// </summary>
        /// <value>Gets or sets the Telemetry sample file path</value>
        public string FullPath { get; set; }
        /// <summary>
        /// Property for the flag indicating, whether the Telemetry sample file is GZiped.
        /// </summary>
        /// <value>Gets or sets the flag indicating, whether the Telemetry sample file is GZiped</value>
        public bool IsZipped { get; set; }

        #endregion
    }
}
