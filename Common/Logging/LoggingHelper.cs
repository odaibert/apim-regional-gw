using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
// Azure stuff
#if !WINDOWS_UWP
using Microsoft.ServiceBus.Messaging;
#endif
using Common.Dto;
using Common.Enums;

namespace Common.Logging
{
    /// <summary>
    /// Class implementing logging functionality.
    /// </summary>
    public static class LoggingHelper
    {
        /// <summary>
        /// Method for sending a logging entry to the dedicated Logging Event Hub.
        /// </summary>
        /// <param name="requestId">Message to log</param>
        /// <param name="componentName">Message to log</param>
        /// <param name="logMessage">Message to log</param>
        /// <param name="logEntryType"></param>
        /// <param name="loggingEventHubConnStr"></param>
        /// <remarks>
        /// The plain log message will be enhanced by a timestamp.
        /// The value provided in <param name="loggingEventHubConnStr"></param> should contain the name of the Event Hub.
        /// </remarks>
        public static async Task WriteLogEntryToEventHubAsync(Guid requestId, string componentName, string logMessage, LogEntryType logEntryType, string loggingEventHubConnStr)
        {
#if !WINDOWS_UWP
            // Create connection to dedicated logging Event Hub
            var eventHubClient = EventHubClient.CreateFromConnectionString(loggingEventHubConnStr);
            try
            {
                // Init Log Entry
                LoggingEntryDto logEntry = new LoggingEntryDto()
                {
                    RequestId = requestId,
                    ComponentName = componentName,
                    Message = logMessage,
                    LogEntryType = logEntryType,
                    Timestamp = DateTime.UtcNow
                };

                // Create JSON data
                JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include
                };
                jsonSettings.Converters.Add(new StringEnumConverter());

                string logEntryMessage = JsonConvert.SerializeObject(logEntry, jsonSettings);

                // Sent logging entry as JSON message to dedicated Event Hub
                await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(logEntryMessage)));
            }
            catch (Exception)
            {
                // TODO: add eception handling
            }

            // Close Event Hub connection
            await eventHubClient.CloseAsync();
#endif
        }
    }
}
