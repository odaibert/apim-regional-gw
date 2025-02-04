using System;

namespace Common.Utilities
{
    /// <summary>
    /// Class implementing static helper/utility methods.
    /// </summary>
    public static class Utilities
    {
        #region Static Members

        private const string PackageDateTimeFormat = "yyyy-MM-ddThh-mm-ss";
        private const string ReportDateTimeFormat = "yyyy-MM-dd";
        private const string GzipedTelemetryPackageExtension = ".gz";
        private const string ProcessedTelemetryReportExtension = ".xml";
        private const string EnrichedGzipedTelemetryPackageSuffix = "_enriched";

        #endregion

        #region Public Methods

        /// <summary>
        /// Helper method for creating the filename for a GZiped Telemetry Package.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <returns>Filename for GZiped Telemetry Package</returns>
        public static string GetGZipedTelemetryPackageFilename(string deviceId)
        {
            return $"{deviceId}_{DateTime.UtcNow.ToString(PackageDateTimeFormat)}{GzipedTelemetryPackageExtension}";
        }

        /// <summary>
        /// Helper method for creating the filename - incl. dir name - for an enriched GZiped Telemetry Package.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="dircetory">Blob container dir name</param>
        /// <returns>Filename for GZiped Telemetry Package</returns>
        public static string GetBlobEnrichedTelemetryPackageFilenameWithDir(string deviceId, string dircetory)
        {
            // If dir name not provided, use devive Id
            if (string.IsNullOrEmpty(dircetory))
            {
                dircetory = deviceId;
            }
            return $"{dircetory}/{deviceId}_{DateTime.UtcNow.ToString(PackageDateTimeFormat)}{EnrichedGzipedTelemetryPackageSuffix}{GzipedTelemetryPackageExtension}";
        }

        /// <summary>
        /// Helper method for creating the filename - incl. dir name - for an enriched GZiped Telemetry Package.
        /// </summary>
        /// <param name="blobName">Base Blob Name</param>
        /// <param name="deviceId">Device Id</param>
        /// <param name="dircetory">Blob container dir name</param>
        /// <returns>Filename for GZiped Telemetry Package</returns>
        public static string GetBlobEnrichedTelemetryPackageFilenameWithDir(string blobName, string deviceId, string dircetory)
        {
            // If dir name not provided, use devive Id
            if (string.IsNullOrEmpty(dircetory))
            {
                dircetory = deviceId;
            }

            if (!string.IsNullOrEmpty(blobName))
            {
                if (blobName.Contains("."))
                {
                    blobName = blobName.Substring(0, blobName.LastIndexOf('.'));
                }
                return $"{blobName}{EnrichedGzipedTelemetryPackageSuffix}{GzipedTelemetryPackageExtension}";
            }

            return GetBlobEnrichedTelemetryPackageFilenameWithDir(deviceId, dircetory);
        }

        /// <summary>
        /// Helper method for creating the filename - incl. dir name - for an enriched GZiped Telemetry Package.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="dircetory">Blob container dir name</param>        
        /// <returns>Filename for GZiped Telemetry Package</returns>
        public static string GetBlobTelemetryPackageFilenameWithDir(string deviceId, string dircetory = null)
        {
            // If dir name not provided, use devive Id
            if (dircetory == null)
            {
                dircetory = deviceId;
            }
            return $"{dircetory}/{deviceId}_{DateTime.UtcNow.ToString(PackageDateTimeFormat)}{GzipedTelemetryPackageExtension}";
        }

        /// <summary>
        /// Helper method for creating the filename - incl. dir name - for an processed Telemetry Report.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="date">Report date</param>
        /// <param name="directory">Blob container dir name</param>
        /// <returns>Filename for processed Telemetry Report</returns>
        public static string GetBlobReportTelemetryPackageFilenameWithDir(string deviceId, DateTime date, string directory = null)
        {
            // If dir name not provided, use devive Id
            if (directory == null)
            {
                directory = deviceId;
            }
            return $"{directory}/{deviceId}_{date.ToString(ReportDateTimeFormat)}{ProcessedTelemetryReportExtension}";
        }

        public static string GetBlobReportTelemetryPackageUri(string blobStorageUrl, string reportFilename)
        {
            return $"{blobStorageUrl}/{reportFilename}";
        }

        #endregion
    }
}
