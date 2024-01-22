using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Function.Domain.Models.OL;
using Function.Domain.Helpers.Parser;
using Function.Domain.Helpers.Logging;

namespace Function.Domain.Services
{
    /// <summary>
    /// Service which filters out OpenLineage events that are not supported by the current version of the function.
    /// </summary>
    public class OlFilter : IOlFilter
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;

        /// <summary>
        /// Constructor for the OlFilter Service
        /// </summary>
        /// <param name="loggerFactory">Passing the logger factory with dependency injection</param>
        public OlFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OlFilter>();
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Filters the OL events to only those that need to be passed on to the event hub
        /// </summary>
        /// <param name="strRequest">This is the OpenLineage data passed from the Spark listener</param>
        /// <returns> 
        /// true: if the message should be passed on for further processing
        /// false: if the message should be filtered out
        /// </returns>
        public bool FilterOlMessage(string strRequest)
        {
            var olEvent = ParseOlEvent(strRequest);
            try
            {
                if (olEvent is not null)
                {
                    var validateEvent = new ValidateOlEvent(_loggerFactory);
                    return validateEvent.Validate(olEvent);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorCodes.OpenLineage.EventValidation, "Error during event validation {strRequest}, {ErrorMessage}", strRequest
                , ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Returns the namespace of the job that the OpenLineage event belongs to
        /// </summary>
        /// <param name="strRequest"> the OpenLineage event </param>
        /// <returns> the namespace value from the event </returns>
        public string GetJobNamespace(string strRequest)
        {
            var olEvent = ParseOlEvent(strRequest);
            return olEvent?.Job.Namespace ?? "";
        }

        private Event? ParseOlEvent(string strEvent)
        {
            try
            {
                var trimString = TrimPrefix(strEvent);
                return JsonConvert.DeserializeObject<Event>(trimString);
            }
            catch (JsonSerializationException ex)
            {
                // TODO Mani
                _logger.LogWarning(ErrorCodes.OpenLineage.JsonSerialization, "Json Serialization Issue: {strEvent}, {ErrorMessage} , {path}", strEvent
                , ex.Message, ex.Path);
            }
            // Parsing error
            catch (Exception ex)
            {
                _logger.LogWarning(ErrorCodes.OpenLineage.ParseEvent, "Unrecognized Message: {strEvent}, {ErrorMessage}", strEvent
            , ex.Message);
            }
            return null;
        }

        private string TrimPrefix(string strEvent)
        {
            return strEvent.Substring(strEvent.IndexOf('{')).Trim();
        }
    }
}