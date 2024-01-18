using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Function.Domain.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Function.Domain.Models.OL;
using System.Linq;
using Function.Domain.Helpers.Logging;

namespace AdbToPurview.Function
{
    public class PurviewOut
    {
        private readonly ILogger<PurviewOut> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOlConsolidateEnrichFactory _olEnrichmentFactory;
        private readonly IOlToPurviewParsingService _olToPurviewParsingService;
        private readonly IPurviewIngestion _purviewIngestion;

        public PurviewOut(ILogger<PurviewOut> logger, IOlToPurviewParsingService olToPurviewParsingService, IPurviewIngestion purviewIngestion, IOlConsolidateEnrichFactory olEnrichmentFactory, ILoggerFactory loggerFactory)
        {
            logger.LogInformation("Enter PurviewOut");
            _logger = logger;
            _loggerFactory = loggerFactory;
            _olEnrichmentFactory = olEnrichmentFactory;
            _olToPurviewParsingService = olToPurviewParsingService;
            _purviewIngestion = purviewIngestion;
        }

        [Function("PurviewOut")]
        // V2 May want to implement batch processing of events by making "input" an array and setting IsBatched
        // see https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-hubs-trigger?tabs=csharp#scaling
        public async Task<string> Run(
            [EventHubTrigger("%EventHubName%", IsBatched = false, Connection = "ListenToMessagesFromEventHub", ConsumerGroup = "%EventHubConsumerGroup%")] string input)
        {
            try
            {
                //Check if event is from Azure Synapse Spark Pools
                // TODO : move this logic into a factory
                if (input.Contains("azuresynapsespark"))
                {
                    var olSynapseEnrichment = _olEnrichmentFactory.CreateEnrichment<EnrichedSynapseEvent>(OlEnrichmentType.Synapse);
                    var olSynapseEvent = await olSynapseEnrichment.ProcessOlMessage(input);
                    if(olSynapseEvent == null || olSynapseEvent.OlEvent == null)
                    {
                        _logger.LogInformation($"Start event, duplicate event, or no context found for Synapse - eventData: {input}");
                        return string.Empty;
                    }

                    _logger.LogInformation($"PurviewOut-ParserService:Processing lineage for Synapse Workspace {olSynapseEvent.OlEvent.Job.Namespace.Split(",").First()}");
                    var purviewSynapseEvent1 = await _olToPurviewParsingService.GetParentEntityAsync(olSynapseEvent);
                    _logger.LogInformation($"PurviewOut-ParserService: {purviewSynapseEvent1}");
                    var jObjectPurviewEvent1 = JsonConvert.DeserializeObject<JObject>(purviewSynapseEvent1!) ?? [];
                    _logger.LogInformation("Calling SendToPurview");
                    await _purviewIngestion.SendToPurview(jObjectPurviewEvent1);

                    var purviewSynapseEvent2 = await _olToPurviewParsingService.GetChildEntityAsync(olSynapseEvent);
                    _logger.LogInformation($"PurviewOut-ParserService: {purviewSynapseEvent2}");
                    var jObjectPurviewEvent2 = JsonConvert.DeserializeObject<JObject>(purviewSynapseEvent2!) ?? [];
                    _logger.LogInformation("Calling SendToPurview");
                    await _purviewIngestion.SendToPurview(jObjectPurviewEvent2);
                }
                else
                {
                    var olConsolodateEnrich = _olEnrichmentFactory.CreateEnrichment<EnrichedEvent>(OlEnrichmentType.Adb);
                    var enrichedEvent = await olConsolodateEnrich.ProcessOlMessage(input);
                    if (enrichedEvent == null)
                    {
                        _logger.LogInformation($"Start event, duplicate event, or no context found - eventData: {input}");
                        return "";
                    }

                    var purviewEvent = _olToPurviewParsingService.GetPurviewFromOlEvent(enrichedEvent);
                    if (purviewEvent == null)
                    {
                        _logger.LogWarning("No Purview Event found");
                        return "unable to parse purview event";
                    }

                    _logger.LogInformation($"PurviewOut-ParserService: {purviewEvent}");
                    var jObjectPurviewEvent = JsonConvert.DeserializeObject<JObject>(purviewEvent) ?? new JObject();
                    _logger.LogInformation("Calling SendToPurview");
                    await _purviewIngestion.SendToPurview(jObjectPurviewEvent);
                }



                return $"Output message created at {DateTime.Now}";
            }
            catch (Exception e)
            {
                LoggingExtensions.LogError(_logger, e, ErrorCodes.PurviewOut.GenericError, "Error in PurviewOut function: {errorMessage}", e.Message);
                return $"Error in PurviewOut function: {e.Message}";
            }
        }
    }
}