using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Function.Domain.Helpers;
using Function.Domain.Services;
using Function.Domain.Providers;


namespace AdbToPurview.Function
{
    public class OpenLineageIn
    {
        private readonly ILogger<OpenLineageIn> _logger;
        private readonly IHttpHelper _httpHelper;

        private const string EH_CONNECTION_STRING = "SendMessagesToEventHub";
        private const string EVENT_HUB_NAME = "EventHubName";

        // The Event Hubs client types are safe to cache and use as a singleton for the lifetime
        // of the application, which is best practice when events are being published or read regularly.
        private EventHubProducerClient _producerClient;
        private IConfiguration _configuration;
        private IOlFilter _olFilter;

        private readonly IOlMessageProvider _olMessageStore;

        public OpenLineageIn(
                ILogger<OpenLineageIn> logger,
                IHttpHelper httpHelper,
                IConfiguration configuration,
                IOlFilter olFilter,
                IOlMessageProvider olMessageStore)
        {
            _logger = logger;
            _httpHelper = httpHelper;
            _configuration = configuration;
            _producerClient = new EventHubProducerClient(_configuration[EH_CONNECTION_STRING], _configuration[EVENT_HUB_NAME]);
            _olFilter = olFilter;
            _olMessageStore = olMessageStore;
        }

        [Function("OpenLineageIn")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                "post",
                Route = "v1/lineage"
            )] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation($"OpenLineageIn: Processing request...");
                // send event data to EventHub
                var events = new List<EventData>();
                
                var strRequest = await req.ReadAsStringAsync();
                if(string.IsNullOrEmpty(strRequest))
                {
                    throw new Exception("OpenLineageIn: Request is null or empty.");
                }

                if (_olFilter.FilterOlMessage(strRequest))
                {
                    _logger.LogInformation($"OpenLineageIn: Request passed validation.");
                    var sendEvent = new EventData(strRequest);
                    var sendEventOptions = new SendEventOptions();
                    // uses the OL Job Namespace as the EventHub partition key
                    var jobNamespace = _olFilter.GetJobNamespace(strRequest);
                    if (jobNamespace == "" || jobNamespace == null)
                    {
                        _logger.LogError($"No Job Namespace found in event: {strRequest}");
                    }
                    else
                    {
                        // log OpenLineage incoming data
                        _logger.LogInformation($"OpenLineageIn: <<{strRequest}>>");

                        // Send to event hub
                        sendEventOptions.PartitionKey = jobNamespace;
                        events.Add(sendEvent);
                        await _producerClient.SendAsync(events, sendEventOptions);

                        if (await _olMessageStore.IsEnabledAsync())
                        {
                            _logger.LogInformation($"OpenLineageIn: Storing incoming file.");
                            // Save to blob storage
                            await _olMessageStore.SaveAsync(strRequest);
                        }
                    }
                }
                else
                {
                    if (await _olMessageStore.IsEnabledAsync())
                    {
                        _logger.LogInformation($"OpenLineageIn: Storing skipped file.");
                        // Save to blob storage
                        await _olMessageStore.SaveAsync(strRequest);
                    }

                    _logger.LogInformation($"OpenLineageIn: Request will be skipped.");
                }
                // Send appropriate success response
                string responseString = "{\"message\":\"Successfully ingested OpenLineage event\"}";
                return await _httpHelper.CreateSuccessfulHttpResponse(req, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpenLineageIn function: {errorMessage}", ex.Message);
                return _httpHelper.CreateServerErrorHttpResponse(req);
            }
        }
    }
}