using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Function.Domain.Models.OL;
using Function.Domain.Services;
using Microsoft.Azure.Amqp;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Function.Domain.Helpers
{
    public class OlSynapseMessageConsolidation : IOlMessageConsolidation
    {
        private ILogger _log;
        private readonly IBlobClientFactory _blobClientFactory;
        private const string CONTAINER_NAME = "ol-synapsemessages";
        public OlSynapseMessageConsolidation(ILoggerFactory loggerFactory, IBlobClientFactory blobClientFactory)
        {
            _log = loggerFactory.CreateLogger<OlSynapseMessageConsolidation>();
            _blobClientFactory = blobClientFactory;
        }

        public async Task<Event?> ConsolidateEventAsync(Event olEvent, string runId)
        {
            try
            {
                //string prefixInput = runId + "/Input/";
                //string prefixOutput = runId + "/Output/";

                string prefixInput = runId + "/Input/";
                string prefixOutput = runId + "/Output/";

                List<Task> inputUploadTasks = new List<Task>();
                List<Task> outputUploadTasks = new List<Task>();

                // Generate GUIDs outside the loop
                List<string> inputBlobNames = Enumerable.Range(0, olEvent.Inputs.Count)
                    .Select(_ => prefixInput + Guid.NewGuid())
                    .ToList();

                List<string> outputBlobNames = Enumerable.Range(0, olEvent.Outputs.Count)
                    .Select(_ => prefixOutput + Guid.NewGuid())
                    .ToList();

                // Parallelize input uploads
                inputUploadTasks.AddRange(olEvent.Inputs.Select((item, index) =>
                {
                    var inputJson = JsonConvert.SerializeObject(item);
                    return _blobClientFactory.UploadAsync(CONTAINER_NAME, inputBlobNames[index], inputJson);
                }));

                // Parallelize output uploads
                outputUploadTasks.AddRange(olEvent.Outputs.Select((item, index) =>
                {
                    var outputJson = JsonConvert.SerializeObject(item);
                    return _blobClientFactory.UploadAsync(CONTAINER_NAME, outputBlobNames[index], outputJson);
                }));

                // Await all upload tasks
                await Task.WhenAll(inputUploadTasks.Concat(outputUploadTasks));


                // Get list of input and outputs which are stored in blob
                List<string> inputs = await _blobClientFactory.GetBlobsByHierarchyAsync(prefixInput);
                List<string> outputs = await _blobClientFactory.GetBlobsByHierarchyAsync(prefixOutput);

                // If there is at least 1 input and 1 output - proceed for message consolidation
                if (inputs.Count > 0 && outputs.Count > 0)
                {
                    List<Inputs?> inputsList = new List<Inputs?>();
                    List<Outputs?> outputsList = new List<Outputs?>();

                    // Get all the inputs
                    List<Task<Inputs?>> inputTasks = inputs.Select(async item =>
                    {
                        var inputObject = await _blobClientFactory.DownloadBlobAsync(CONTAINER_NAME, item);
                        return JsonConvert.DeserializeObject<Inputs?>(inputObject);
                    }).ToList();

                    // Get all the outputs
                    List<Task<Outputs?>> outputTasks = outputs.Select(async item =>
                    {
                        var outputObject = await _blobClientFactory.DownloadBlobAsync(CONTAINER_NAME, item);
                        return JsonConvert.DeserializeObject<Outputs?>(outputObject);
                    }).ToList();

                    inputsList = (await Task.WhenAll(inputTasks)).Where(input => input != null).ToList();
                    outputsList = (await Task.WhenAll(outputTasks)).Where(output => output != null).ToList();

                    // Message Consolidation - Update inputs / outputs to the current olevent object
                    if (inputsList.Count > 0)
                    {
                        olEvent.Inputs = inputsList;
                    }

                    if (outputsList.Count > 0)
                    {
                        olEvent.Outputs = outputsList;
                    }
                    // TO DO check mani - parallel foreach
                    // TO DO check mani -  any uniqueness in input and output
                    // if we have same event same i/o it will add , any error further
                    // Do we need to do distinct on names of input or output etc
                    return olEvent;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"OlSynapseMessageConsolidation-ConsolidateEventAsync: Error {ex.Message} ");
                throw;  // TO DO check mani - throw if there is any error?
            }
        }
    }
}