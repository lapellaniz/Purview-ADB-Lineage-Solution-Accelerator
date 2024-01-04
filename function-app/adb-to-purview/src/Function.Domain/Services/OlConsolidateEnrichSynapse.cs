using System;
using System.Linq;
using System.Threading.Tasks;
using Function.Domain.Helpers;
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
        private readonly IBlobProvider _blobProvider;

        /// <summary>
        /// Constructs the OlConsolodateEnrich object from the Function framework using DI
        /// </summary>
        /// <param name="loggerFactory">Logger Factory to support DI from function framework or code calling helper classes</param>
        /// <param name="configuration">Function framework config from DI</param>
        public OlConsolidateEnrichSynapse(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            ISynapseClientProvider synapseClientProvider,
            IBlobProvider blobProvider)
        {
            _logger = loggerFactory.CreateLogger<OlConsolidateEnrichSynapse>();
            _loggerFactory = loggerFactory;
            _configuration = configuration;
            _synapseClientProvider = synapseClientProvider;
            _blobProvider = blobProvider;
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
                // Deserialize the event
                _event = JsonConvert.DeserializeObject<Event>(trimString) ?? new Event();
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogWarning(ex, $"Unrecognized Message: {strEvent}, error: {ex.Message} path: {ex.Path}");
            }

            // Validate the event
            var validateOlEvent = new ValidateOlEvent(_loggerFactory);
            if (!validateOlEvent.Validate(_event))
            {
                return null;
            }

            try
            {
                // Enrich the event with Synapse information

                // WORKSPACE_NAME,synapseKind
                var workspaceName = _event.Job.Namespace.Split(",").First();
                // NOTEBOOK_NAME_sparkPoolName_sparkApplicationId.sparkPlanId
                var jobNameParts = _event.Job.Name.Split(".");
                var jobSynapseName = jobNameParts.First();
                var jobSynapseNameParts = jobSynapseName.Split("_");
                var sparkPoolName = string.Empty;
                var sparkApplicationId = string.Empty;
                var notebookName = string.Empty;

                // Message Consolidation
                var olSynapseMessageConsolidation = new OlSynapseMessageConsolidation(_loggerFactory, _blobProvider);
                _event = await olSynapseMessageConsolidation.ConsolidateEventAsync(_event, jobSynapseName);

                if (_event == null)
                {
                    return null;
                }

                // Enrich Job Details
                if (jobSynapseNameParts.Length > 1)
                {
                    sparkApplicationId = jobSynapseNameParts.Last();
                    sparkPoolName = jobSynapseNameParts[^2];
                    notebookName = jobSynapseName.Substring(0, jobSynapseName.IndexOf(sparkPoolName) - 1);
                }

                // Enrich RunId
                if (!long.TryParse(sparkApplicationId, out long runId))
                {
                    throw new Exception($"Unable to parse runId '{sparkApplicationId}' from job name: {_event.Job.Name}");
                }

                // Get Synapse Job and Spark Pool Details
                SynapseRoot? synapseRoot = await _synapseClientProvider.GetSynapseJobAsync(runId, workspaceName);
                SynapseSparkPool? synapseSparkPool = await _synapseClientProvider.GetSynapseSparkPoolsAsync(workspaceName, sparkPoolName);

                // Enrich Notebook Name from Synapse Job Details
                if (synapseRoot != null)
                {
                    var sparkJob = synapseRoot?.SparkJobs?.FirstOrDefault();
                    if (sparkJob != null)
                    {
                        _logger.LogInformation("Using SynapseRoot for enrichment: {sparkJobName}", sparkJob.Name);
                        //NOTEBOOK_NAME_sparkPoolName_sparkApplicationId
                        var sparkJobName = sparkJob.Name ?? string.Empty;
                        var sparkJobNameParts = sparkJobName.Split("_");
                        var sparkJobPoolName = sparkJob.SparkPoolName ?? sparkJobNameParts[^2];
                        if (sparkJobNameParts.Length > 1)
                        {
                            notebookName = sparkJobName.Substring(0, sparkJobName.IndexOf(sparkJobPoolName) - 1);
                        }
                    }
                }

                // TODO : Validate notebookname and expected vars are set.

                return new EnrichedSynapseEvent(_event, synapseRoot, synapseSparkPool)
                {
                    OlJobWorkspace = workspaceName,
                    SparkPoolName = sparkPoolName,
                    SparkApplicationId = sparkApplicationId,
                    NotebookName = notebookName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating enriched event for job '{jobId}': {errorMessage}", _event.Job.Name, ex.Message);
                throw;
            }
        }

        private string TrimPrefix(string strEvent)
        {
            return strEvent.Substring(strEvent.IndexOf('{')).Trim();
        }
    }
}