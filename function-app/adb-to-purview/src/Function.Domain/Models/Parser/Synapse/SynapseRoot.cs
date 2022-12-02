using Newtonsoft.Json;

namespace Function.Domain.Models.SynapseSpark
{
    public class SynapseRoot
    {
        [JsonProperty("nJobs")]
        public int? JobsCount = 0;


        [JsonProperty("sparkJobs")]
        public SynapseSparkJobTask[]? SparkJobs = null;

    }


    public class SynapseSparkPool
    {
        [JsonProperty("properties")]
        public synapseSparkPoolProperties? Properties = null;

    }

    public class synapseSparkPoolProperties
    {
        [JsonProperty("sparkVersion")]
        public string? SparkVersion = "";


    }
}