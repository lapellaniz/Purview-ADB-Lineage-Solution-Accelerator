using System.Collections.Generic;
using Newtonsoft.Json;

namespace Function.Domain.Models.SynapseSpark
{
    public class SynapseSparkJobTask
    {
        [JsonProperty("state")]
        public string? State = "";
        [JsonProperty("name")]
        public string? Name = null;
        [JsonProperty("submitter")]
        public string? Submitter = null;
        [JsonProperty("compute")]
        public string? Compute = null;
        [JsonProperty("sparkPoolName")]
        public string? SparkPoolName = null;
        [JsonProperty("sparkApplicationId")]
        public string? SparkApplicationId = null;
        [JsonProperty("livyId")]
        public long? LivyId = null;
        
        [JsonProperty("sparkJobDefinition")]
        public string? SparkJobDefinition = null;

        [JsonProperty("pipeline")]
        public string? Pipeline = null;
        [JsonProperty("jobType")]
        public string? JobType = null;

        [JsonProperty("submitTime")]
        public string? SubmitTime = null;
        [JsonProperty("endTime")]
        public string? EndTime = null;
        [JsonProperty("queuedDuration")]
        public string? QueuedDuration = null;
        [JsonProperty("runningDuration")]
        public string? RunningDuration = null;

        [JsonProperty("isJobTimedOut")]
        public string? IsJobTimedOut = null;

    }
}