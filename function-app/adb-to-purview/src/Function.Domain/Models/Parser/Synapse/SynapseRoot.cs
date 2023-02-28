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

    public class SynapseSparkNoteBook
    {
        [JsonProperty("properties")]
        public synapseSparkNotebookProperties? Properties = null;

    }

    public class synapseSparkNotebookProperties
    {
        [JsonProperty("cells")]
        public SparkNotebookCell[]? Cells = null;

    }

    public class SparkNotebookCell
    {
        [JsonProperty("cell_type")]
        public string? CellType = "";

        [JsonProperty("source")]
        public string[] Source = new string[0];


    }

    public class OpenAICompletionResponse
    {
        [JsonProperty("choices")]
        public OpenAICompletionChoice[]? Choices = null;

    }

    public class OpenAICompletionChoice
    {
        [JsonProperty("text")]
        public string? Text = "";

    }
}