using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Function.Domain.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Function.Domain.Models.OL;

namespace AdbToPurview.Function
{
    public class PurviewOut
    {
        private readonly ILogger<PurviewOut> _logger; 
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOlConsolodateEnrich _olConsolodateEnrich;       
        private readonly IOlToPurviewParsingService _olToPurviewParsingService;
        private readonly IPurviewIngestion _purviewIngestion;

        private Event _event = new Event();
        public PurviewOut(ILogger<PurviewOut> logger, IOlToPurviewParsingService olToPurviewParsingService, IPurviewIngestion purviewIngestion, IOlConsolodateEnrich olConsolodateEnrich, ILoggerFactory loggerFactory)
        {
            logger.LogInformation("Enter PurviewOut");
            _logger = logger; 
            _loggerFactory = loggerFactory;
            _olConsolodateEnrich = olConsolodateEnrich;           
            _olToPurviewParsingService = olToPurviewParsingService;
            _purviewIngestion = purviewIngestion;
        }

        [Function("PurviewOut")]
        // V2 May want to implement batch processing of events by making "input" an array and setting IsBatched
        // see https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-hubs-trigger?tabs=csharp#scaling
        public async Task<string> Run(
            [EventHubTrigger("%EventHubName%", IsBatched = false ,Connection = "ListenToMessagesFromEventHub", ConsumerGroup = "%EventHubConsumerGroup%")] string input)
        {
            try{
                var trimString = input.Substring(input.IndexOf('{')).Trim();
                _event = JsonConvert.DeserializeObject<Event>(trimString) ?? new Event();

                //Check if event is from Azure Synapse Spark Pools
                if(_event.Job.Namespace.Contains("azuresynapsespark") && _event.EventType == "COMPLETE")
                {
                    _logger.LogInformation($"PurviewOut-ParserService:Processing lineage for Synapse Workspace {_event.Job.Namespace.Split(",")[0]}");
                    var purviewSynapseEvent1 = _olToPurviewParsingService.GetParentEntity(_event);
                    _logger.LogInformation($"PurviewOut-ParserService: {purviewSynapseEvent1}");
                    var jObjectPurviewEvent1 = JsonConvert.DeserializeObject<JObject>(purviewSynapseEvent1!) ?? new JObject();
                    _logger.LogInformation("Calling SendToPurview");
                    await _purviewIngestion.SendToPurview(jObjectPurviewEvent1);

                    var purviewSynapseEvent2 = _olToPurviewParsingService.GetChildEntity(_event);
                    _logger.LogInformation($"PurviewOut-ParserService: {purviewSynapseEvent2}");
                    var jObjectPurviewEvent2 = JsonConvert.DeserializeObject<JObject>(purviewSynapseEvent2!) ?? new JObject();
                    _logger.LogInformation("Calling SendToPurview");
                    await _purviewIngestion.SendToPurview(jObjectPurviewEvent2);
                }
                else
                {
                    var enrichedEvent = await _olConsolodateEnrich.ProcessOlMessage(input);
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
            catch(Exception e){
                var message = $"Error in PurviewOut function: {e.Message}";
                _logger.LogError(message);
                return message;
            }
        }
    }
}