using System;
using Microsoft.Extensions.Logging;

namespace Function.Domain.Helpers.Logging
{
    public static class LoggingExtensions
    {
        public static void LogError(this ILogger logger, Exception ex, int errorCode, string message, params object[] args)
        {
            var logMessage = "Error Code: {code}. Message: " + message;
            logger.LogError(ex, logMessage, errorCode, args);
        }

        public static void LogWarning(this ILogger logger, int warningCode, string message, params object[] args)
        {
            var logMessage = "Warning Code: {code}. Message: " + message;
            logger.LogWarning(logMessage, warningCode, message, args);
        }
    }
}