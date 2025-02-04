using System;
// Own usings
using Common.Enums;

namespace Common.Dto
{
    /// <summary>
    /// Class implementing a Log Entry DTO.
    /// </summary>
    /// <remarks>
    /// This class is used for serialization/desiarialization of Logging Data, to be send to an dedicated Event Hub.
    /// </remarks>
    public class LoggingEntryDto
    {
        /// <summary>
        /// Proeprty for the request Id.
        /// </summary>
        /// <value>Gets or sets the Request Id</value>
        /// <value>
        /// The Request Id will be used to consolidate the various log entries for a specific request/session.
        /// The Id value should be set at a start of an action/operation within the Component having created the log entry.
        /// </value>
        public Guid RequestId { get; set; }
        /// <summary>
        /// Proeprty for the name of the Component which has created the Log Entry.
        /// </summary>
        /// <value>Gets or sets the name of the Component which has created the Log Entry</value>
        public string ComponentName { get; set; }
        /// <summary>
        /// Property for the logging entry type.
        /// </summary>
        /// <value>Gets or sets the logging entry type</value>
        /// <remarks>
        /// Logging Entry types are: <see cref="LogEntryType"/>
        /// </remarks>
        public LogEntryType LogEntryType { get; set; }
        /// <summary>
        /// Proeprty for the logging message.
        /// </summary>
        /// <value>Gets or sets the logging message</value>
        public string Message { get; set; }
        /// <summary>
        /// Proeprty for the logging entry timestamp.
        /// </summary>
        /// <value>Gets or sets the logging entry timestamp</value>
        /// <remarks>
        /// The timestamp is given as UTC.
        /// </remarks>
        public DateTime Timestamp { get; set; }
    }
}
