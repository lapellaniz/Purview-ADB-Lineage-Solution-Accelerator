using System;
using System.Linq;
using System.Threading.Tasks;
using Function.Domain.Helpers.Parser;
using Function.Domain.Models.OL;
using Function.Domain.Models.SynapseSpark;
using Function.Domain.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Function.Domain.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class OlConsolidateEnrichSynapse : IOlConsolodateEnrich<EnrichedSynapseEvent>
    {
        private const string START_EVENT_TYPE = "START";
        private const string COMPLETE_EVENT_TYPE = "COMPLETE";
        private readonly ILogger<OlConsolidateEnrichSynapse> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private Event _event = new Event();
        private ISynapseClientProvider _synapseClientProvider;

        /// <summary>
        /// Constructs the OlConsolodateEnrich object from the Function framework using DI
        /// </summary>
        /// <param name="loggerFactory">Logger Factory to support DI from function framework or code calling helper classes</param>
        /// <param name="configuration">Function framework config from DI</param>
        public OlConsolidateEnrichSynapse(
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<OlConsolidateEnrichSynapse>();
            _loggerFactory = loggerFactory;
            _configuration = configuration;
            // TODO : Inject via DI
            _synapseClientProvider = new SynapseClientProvider(loggerFactory, _configuration);
        }
        public string GetJobNamespace()
        {
            return _event.Job.Namespace;
        }

        public async Task<EnrichedSynapseEvent?> ProcessOlMessage(string strEvent)
        {
            var trimString = TrimPrefix(strEvent);
            try
            {
                _event = JsonConvert.DeserializeObject<Event>(trimString) ?? new Event();
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogWarning(ex, $"Unrecognized Message: {strEvent}, error: {ex.Message} path: {ex.Path}");
            }

            var validateOlEvent = new ValidateOlEvent(_loggerFactory);

            // Validate the event
            if (!validateOlEvent.Validate(_event))
            {
                return null;
            }

            SynapseRoot? synapseRoot = await GetSynapseJobAsync(_event);
            SynapseSparkPool? synapseSparkPool = await GetSynapseSparkPoolAsync(_event);

            return new EnrichedSynapseEvent(_event, synapseRoot, synapseSparkPool);
        }

        private async Task<SynapseRoot?> GetSynapseJobAsync(Event eEvent)
        {
            string runId = eEvent.Job.Name.Split(".")[0].Split("_")[eEvent.Job.Name.Split(".")[0].Split("_").Length - 1];
            return await _synapseClientProvider.GetSynapseJobAsync(long.Parse(runId), eEvent.Job.Namespace.Split(",")[0]);            
        }

        private async Task<SynapseSparkPool?> GetSynapseSparkPoolAsync(Event eEvent)
        {
            string sparkJobName = eEvent.Job.Name.Split(".")[0].Split("_")[eEvent.Job.Name.Split(".")[0].Split("_").Length - 1];
            string sparkNoteBookName = eEvent.Job.Name.Substring(0, eEvent.Job.Name.IndexOf(sparkJobName) - 1);
            string sparkClusterName = sparkNoteBookName.Split("_").Last();
            return await _synapseClientProvider.GetSynapseSparkPoolsAsync(eEvent.Job.Namespace.Split(",")[0], sparkClusterName);
        }

        private string TrimPrefix(string strEvent)
        {
            return strEvent.Substring(strEvent.IndexOf('{')).Trim();
        }
    }
}