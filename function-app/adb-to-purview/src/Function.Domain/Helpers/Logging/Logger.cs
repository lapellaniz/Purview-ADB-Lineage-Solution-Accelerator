using Microsoft.Extensions.Logging;

namespace Function.Domain.Helpers
{
    public class Logger
    {
        private readonly ILogger<Logger> _logger;

        public Logger(ILogger<Logger> logger)
        {
            _logger = logger;
        }

        public void LogInformation(CustomLogging information)
        {
            // Log the information with necessary details
            _logger.LogInformation($"Message: {information.Message},Custom Message: {information.CustomMessage}, Custom Data: {information.CustomData}");
        }

        public void LogError(CustomLogging exception)
        {
            // Log the exception with necessary details
            _logger.LogError(exception, $"Error Code: {exception.ErrorCode}, Message: {exception.Message}, Custom Message: {exception.CustomMessage}, Custom Data: {exception.CustomData}");
        }

        public void LogWarning(CustomLogging warning)
        {
            // Log the warning with necessary details
            _logger.LogWarning($"Warning Code: {warning.ErrorCode}, Message: {warning.Message}, Custom Message: {warning.CustomMessage}, Custom Data: {warning.CustomData}");
        }
    }
}